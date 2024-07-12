// AccelBuilder.cs

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using PathTracingStruct;

namespace AccelBuilder
{
    public static class BVHBuilder
    {
        // Hyperparameters of BVH Construction
        private const int BucketCount = 12;
        private const float InteriorCost = 0.25f;
        private const float LeafCost = 1.0f;
        private const int maxBVHDepth = 32;

        [StructLayout(LayoutKind.Sequential)]
        public struct Bound3
        {
            public Vector3 pMin;
            public Vector3 pMax;

            public static Bound3 Bound3Init()
            {
                Bound3 bound = new Bound3(new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity),
                                          new Vector3(Mathf.NegativeInfinity, Mathf.NegativeInfinity, Mathf.NegativeInfinity));
                return bound;
            }

            public Bound3(Vector3 pMin, Vector3 pMax)
            {
                this.pMin = pMin;
                this.pMax = pMax;
            }

            public bool IsValid()
            {
                return pMax.x >= pMin.x && pMax.y >= pMin.y && pMax.z >= pMin.z;
            }

            public Vector3 Diagnal()
            {
                return pMax - pMin;
            }

            public int MaxExtentDimension()
            {
                Vector3 diagnal = Diagnal();
                return diagnal.x > diagnal.y ? (diagnal.x > diagnal.z ? 0 : 2) : (diagnal.y > diagnal.z ? 1 : 2);
            }

            public float SurfaceArea()
            {
                Vector3 diagnal = Diagnal();
                return 2.0f * (diagnal.x * diagnal.y + diagnal.y * diagnal.z + diagnal.x * diagnal.z);
            }

            public Vector3 Offset(Vector3 p)
            {
                Vector3 pOffset = p - pMin;
                Vector3 diagnal = pMax - pMin;
                return new Vector3(pOffset.x / diagnal.x, pOffset.y / diagnal.y, pOffset.z / diagnal.z);
            }

            public Bound3 Union(Bound3 bound)
            {
                pMin = Vector3.Min(pMin, bound.pMin);
                pMax = Vector3.Max(pMax, bound.pMax);
                return this;
            }

            public Bound3 Union(Vector3 p)
            {
                pMin = Vector3.Min(pMin, p);
                pMax = Vector3.Max(pMax, p);
                return this;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LinearBVHNode
        {
            public Bound3 bound;

            // Primitive Offset for Leaf Node, Second Child Offset for Interior Node
            public int offset;

            // Zero for Interior Node
            public int primtiveCount;

            // Zero for Leaf Node
            public int splitAxis;

            public static LinearBVHNode InteriorInit(Bound3 bound, int secondChildOffset, int splitAxis)
            {
                LinearBVHNode node = new LinearBVHNode();
                node.bound = bound;
                node.offset = secondChildOffset;
                node.primtiveCount = 0;
                node.splitAxis = splitAxis;
                return node;
            }

            public static LinearBVHNode LeafInit(Bound3 bound, int primitiveOffset, int primitiveCount)
            {
                LinearBVHNode node = new LinearBVHNode();
                node.bound = bound;
                node.offset = primitiveOffset;
                node.primtiveCount = primitiveCount;
                node.splitAxis = 0;
                return node;
            }
        }

        private struct BVHNode
        {
            public Bound3 bound;
            public BVHNode[] children;

            // Zero for Interior Node
            public int primitiveOffset;

            // Zero for Interior Node
            public int primitiveCount;

            // Zero for Leaf Node
            public int splitAxis;

            public static BVHNode InteriorNodeInit(Bound3 bound, BVHNode[] children, int splitAxis)
            {
                BVHNode bvhNode = new BVHNode();
                bvhNode.bound = bound;
                bvhNode.children = children;
                bvhNode.primitiveOffset = 0;
                bvhNode.primitiveCount = 0;
                bvhNode.splitAxis = splitAxis;
                return bvhNode;
            }

            public static BVHNode LeafNodeInit(Bound3 bound, int primitiveOffset, int primitiveCount)
            {
                BVHNode bvhNode = new BVHNode();
                bvhNode.bound = bound;
                bvhNode.children = null;
                bvhNode.primitiveOffset = primitiveOffset;
                bvhNode.primitiveCount = primitiveCount;
                bvhNode.splitAxis = 0;
                return bvhNode;
            }
        }

        private struct PrimitiveInfo
        {
            public int primitiveIndex;
            public Bound3 bound;
            public Vector3 centroid;

            public PrimitiveInfo(int primitiveIndex, Bound3 bound, Vector3 centroid)
            {
                this.primitiveIndex = primitiveIndex;
                this.bound = bound;
                this.centroid = centroid;
            }
        }

        private struct Bucket
        {
            public int primitiveCount;
            public Bound3 bound;

            public Bucket(int primitiveCount, Bound3 bound)
            {
                this.primitiveCount = primitiveCount;
                this.bound = bound;
            }
        }

        public static LinearBVHNode[] BuildBVHTree(PathTracingPrimitive[] primitives)
        {
            Bound3 worldBound = Bound3.Bound3Init();
            PrimitiveInfo[] primitiveInfos = new PrimitiveInfo[primitives.Length];

            for (int i = 0; i < primitives.Length; i++)
            {
                Bound3 bound = Bound3.Bound3Init();
                Vector3 centroid = new Vector3();
                for (int n = 0; n < 3; n++)
                {
                    bound.Union(primitives[i].shape[n]);
                    centroid += primitives[i].shape[n];
                }
                centroid /= 3.0f;
                worldBound.Union(bound);

                primitiveInfos[i] = new PrimitiveInfo(i, bound, centroid);
            }

            int nodeCount = 0;
            BVHNode root = RecursiveBuildBVHTree(primitiveInfos, 0, primitives.Length, ref nodeCount, 0);
            ReorderPrimitives(primitiveInfos, primitives);
            return FlattenBVHTree(root, nodeCount);
        }

        private static BVHNode RecursiveBuildBVHTree(PrimitiveInfo[] primitiveInfos,
                                                     int firstPrimitive,
                                                     int primitiveCount,
                                                     ref int nodeCount,
                                                     int depth)
        {
            // Increase Depth
            depth++;

            // Create Node Bound
            Bound3 nodeBound = Bound3.Bound3Init();
            for (int i = firstPrimitive; i < firstPrimitive + primitiveCount; i++)
            {
                nodeBound.Union(primitiveInfos[i].bound);
            }

            // Create Leaf Node if Node Count Less Than 4
            if (primitiveCount <= 2 || depth >= maxBVHDepth)
            {
                nodeCount++;
                return BVHNode.LeafNodeInit(nodeBound, firstPrimitive, primitiveCount);
            }
            else
            {
                // Create Centroid Bound
                Bound3 centroidBound = Bound3.Bound3Init();
                for (int i = firstPrimitive; i < firstPrimitive + primitiveCount; i++)
                {
                    centroidBound.Union(primitiveInfos[i].centroid);
                }

                // Initialize Buckets
                List<PrimitiveInfo>[] primitiveBuckets = new List<PrimitiveInfo>[BucketCount];
                Bucket[] buckets = new Bucket[BucketCount];
                for (int i = 0; i < BucketCount; i++)
                {
                    primitiveBuckets[i] = new List<PrimitiveInfo>();
                    buckets[i].bound = Bound3.Bound3Init();
                }

                // Get the Dimension that Has the Maximum Extent
                int maxDim = centroidBound.MaxExtentDimension();

                // Create Leaf Node if Impossible to Split
                if (centroidBound.pMax[maxDim] == centroidBound.pMin[maxDim])
                {
                    nodeCount++;
                    return BVHNode.LeafNodeInit(nodeBound, firstPrimitive, primitiveCount);
                }
                
                // Create Buckets
                for (int i = firstPrimitive; i < firstPrimitive + primitiveCount; i++)
                {
                    int bucketIndex = Math.Min((int)(nodeBound.Offset(primitiveInfos[i].centroid)[maxDim] * BucketCount), BucketCount - 1);
                    primitiveBuckets[bucketIndex].Add(primitiveInfos[i]);
                    buckets[bucketIndex].primitiveCount++;
                    buckets[bucketIndex].bound.Union(primitiveInfos[i].bound);
                }

                // Do a Forward Scan for Buckets
                Bound3[] forwardBound = new Bound3[BucketCount - 1];
                int[] leftPrimitiveCount = new int[BucketCount - 1];
                forwardBound[0] = buckets[0].bound;
                leftPrimitiveCount[0] = buckets[0].primitiveCount;
                for (int i = 1; i < BucketCount - 1; i++)
                {
                    forwardBound[i] = forwardBound[i - 1];
                    forwardBound[i].Union(buckets[i].bound);
                    leftPrimitiveCount[i] = buckets[i].primitiveCount + leftPrimitiveCount[i - 1];
                }

                // Do a Backward Scan for Buckets
                Bound3[] backwardBound = new Bound3[BucketCount - 1];
                backwardBound[BucketCount - 2] = buckets[BucketCount - 1].bound;
                for (int i = BucketCount - 3; i >= 0; i--)
                {
                    backwardBound[i] = backwardBound[i + 1];
                    backwardBound[i].Union(buckets[i + 1].bound);
                }

                // Evaluate Costs Using SAH
                float minCost = Mathf.Infinity;
                int minCostSplit = 0;
                for (int i = 0; i < BucketCount - 1; i++)
                {
                    float cost;
                    Bound3 left = forwardBound[i];
                    Bound3 right = backwardBound[i];
                    if (left.IsValid() && right.IsValid())
                    {
                        cost = InteriorCost + LeafCost * (
                                   left.SurfaceArea() * leftPrimitiveCount[i]
                                 + right.SurfaceArea() * (primitiveCount - leftPrimitiveCount[i])
                               ) / nodeBound.SurfaceArea();
                    }
                    else
                    {
                        cost = InteriorCost + LeafCost * primitiveCount;
                    }
                    if (cost < minCost)
                    {
                        minCost = cost;
                        minCostSplit = i;
                    }
                }

                // Create Leaf Node If the Min Cost Split Doesn't Actually Split Anything
                if (leftPrimitiveCount[minCostSplit] == 0 || primitiveCount - leftPrimitiveCount[minCostSplit] == 0)
                {
                    nodeCount++;
                    return BVHNode.LeafNodeInit(nodeBound, firstPrimitive, primitiveCount);
                }

                // Reorder the PrimitiveInfos Array
                int offset = 0;
                for (int i = 0; i < BucketCount; i++)
                {
                    for (int j = 0; j < primitiveBuckets[i].Count; j++)
                    {
                        primitiveInfos[firstPrimitive + offset] = primitiveBuckets[i][j];
                        offset++;
                    }
                }

                // Create Interior Node
                int leftChildFirstPrimitive = firstPrimitive;
                int leftChildPrimitiveCount = leftPrimitiveCount[minCostSplit];
                int rightChildFirstPrimitive = firstPrimitive + leftChildPrimitiveCount;
                int rightChildPrimitiveCount = primitiveCount - leftChildPrimitiveCount;

                nodeCount++;
                return BVHNode.InteriorNodeInit(nodeBound,
                                                new BVHNode[] {
                                                    RecursiveBuildBVHTree(primitiveInfos, leftChildFirstPrimitive, leftChildPrimitiveCount, ref nodeCount, depth),
                                                    RecursiveBuildBVHTree(primitiveInfos, rightChildFirstPrimitive, rightChildPrimitiveCount, ref nodeCount, depth)
                                                },
                                                maxDim);
            }
        }

        private static void ReorderPrimitives(PrimitiveInfo[] primitiveInfos, PathTracingPrimitive[] primitives)
        {
            PathTracingPrimitive[] reorderedPrimitives = new PathTracingPrimitive[primitiveInfos.Length];
            for (int i = 0; i < primitiveInfos.Length; i++)
            {
                reorderedPrimitives[i] = primitives[primitiveInfos[i].primitiveIndex];
            }
            for (int i = 0; i < reorderedPrimitives.Length; i++)
            {
                primitives[i] = reorderedPrimitives[i];
            }
        }

        private static LinearBVHNode[] FlattenBVHTree(BVHNode root, int nodeCount)
        {
            LinearBVHNode[] linearBVHNodes = new LinearBVHNode[nodeCount];
            int offset = 0;
            RecursiveFlattenBVHTree(linearBVHNodes, root, ref offset);
            return linearBVHNodes;
        }

        private static void RecursiveFlattenBVHTree(LinearBVHNode[] nodeBuffer, BVHNode node, ref int offset)
        {
            if (node.primitiveCount == 0)
            {
                int currentOffset = offset;
                offset++;
                RecursiveFlattenBVHTree(nodeBuffer, node.children[0], ref offset);
                nodeBuffer[currentOffset] = LinearBVHNode.InteriorInit(node.bound, offset, node.splitAxis);
                RecursiveFlattenBVHTree(nodeBuffer, node.children[1], ref offset);
            }
            else
            {
                nodeBuffer[offset] = LinearBVHNode.LeafInit(node.bound, node.primitiveOffset, node.primitiveCount);
                offset++;
            }
        }
    }

} // namespace AccelBuilder