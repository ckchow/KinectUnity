using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;

// I screwed with this so it can use mathnet.numerics

// if TField is not a double then I don't know what's going to happen.
namespace KdTree
{
    /// <summary>
    /// Represents a kd-tree data structure, used for the binary partitioning of k-dimensional space.
    /// </summary>
    /// <typeparam name="TValue">The type of the value of the contained nodes.</typeparam>
    /// <typeparam name="TField">The type of the underlying field of the vector space of locations.</typeparam>
    public class KdTree<TValue> : ICollection<TValue>
        //where TField : struct, IEquatable<TField>, IFormattable
    {
        // Default location getter function.
        // modified this business to use normal vectors
        private static readonly Func<TValue, Vector> defaultLocationGetter =
            el => (Vector)(object)el;

        // Compares values of underlying field of vector space of locations.
        private static readonly IComparer<double> fieldComparer = Comparer<double>.Default;

        // Performs arithmetic on values of field type.
        private static readonly IArithmetic<double> fieldArithmetic = Arithmetic<double>.Default;

        /// <summary>
        /// Constructs a kd-tree from the specified collection of elements.
        /// </summary>
        /// <param name="dimensionality">The dimensionality of each location vector.</param>
        /// <param name="elements">The collection of elements from which to create the tree.</param>
        /// <param name="locationGetter">A function that gets the location of a given element in kd-space.</param>
        /// <returns>The root node of the constructed kd-tree.</returns>
        public static KdTree<TValue> Construct(int dimensionality, IEnumerable<TValue> elements,
            Func<TValue, Vector> locationGetter = null)
        {
            if (elements == null)
                throw new ArgumentNullException("elements");

            // Create and initialize kd-tree.
            var tree = new KdTree<TValue>();
            tree.dimensionality = dimensionality;
            if (locationGetter != null)
                tree.locationGetter = locationGetter;

            // Construct nodes of tree.
            var elementsArray = elements.ToArray();
            tree.root = Construct(tree, elementsArray, elementsArray.GetLowerBound(0),
                elementsArray.GetUpperBound(0), 0, new ValueLocationComparer(tree.locationGetter));

            return tree;
        }

        private static KdTreeNode<TValue> Construct(KdTree<TValue> tree, TValue[] elements, int startIndex,
            int endIndex, int depth, ValueLocationComparer valueLocationComparer)
        {
            var length = endIndex - startIndex + 1;
            if (length == 0)
                return null;

            // Sort array of elements by component of chosen dimension, in ascending magnitude.
            valueLocationComparer.Dimension = depth % tree.dimensionality;
            Array.Sort(elements, startIndex, length, valueLocationComparer);

            // Select median element as pivot.
            var medianIndex = startIndex + length / 2;
            var medianElement = elements[medianIndex];

            // Create node and construct sub-trees around pivot element.
            var node = new KdTreeNode<TValue>(medianElement);
            node.LeftChild = Construct(tree, elements, startIndex, medianIndex - 1, depth + 1, valueLocationComparer);
            node.RightChild = Construct(tree, elements, medianIndex + 1, endIndex, depth + 1, valueLocationComparer);

            return node;
        }

        // Dimensionality of location vectors.
        private int dimensionality;

        // Root node of tree.
        private KdTreeNode<TValue> root;

        // Function that returns location vector of given element.
        private Func<TValue, Vector> locationGetter;

        /// <summary>
        /// Initializes a new instance of the <see cref="KdTree{TValue, TField}"/> class with the specified root node.
        /// </summary>
        /// <param name="dimensionality">The dimensionality of the kd-space.</param>
        /// <param name="root">The root node of the tree.</param>
        /// <param name="locationGetter">A function that returns the location of a given element in kd-space.</param>
        public KdTree(int dimensionality, KdTreeNode<TValue> root, Func<TValue, Vector> locationGetter = null)
            : this()
        {
            if (root != null)
                throw new ArgumentNullException("root");

            this.dimensionality = dimensionality;
            this.root = root;
            if (locationGetter != null)
                this.locationGetter = locationGetter;
        }

        private KdTree()
        {
            this.locationGetter = defaultLocationGetter;
        }

        /// <summary>
        /// Gets the root node of the tree.
        /// </summary>
        /// <value>The root node.</value>
        public KdTreeNode<TValue> Root
        {
            get { return this.root; }
        }

        /// <summary>
        /// Gets the function that returns the location of a given element in kd-space.
        /// </summary>
        /// <value>The location getter function.</value>
        public Func<TValue, Vector> LocationGetter
        {
            get { return this.locationGetter; }
        }

        /// <summary>
        /// Finds all nodes in the tree that lie within the specified range of a location.
        /// </summary>
        /// <param name="location">The location for which to find the nearest node.</param>
        /// <param name="range">The range in which to search for nodes.</param>
        /// <returns>A collection of nodes with distance from <paramref name="location"/> less than
        /// <paramref name="range"/>.</returns>
        public IEnumerable<TValue> FindInRange(Vector location, double range)
        {
            if (location == null)
                throw new ArgumentNullException("location");

            var nodesList = new List<TValue>();
            FindInRange(location, this.root, range, nodesList, 0);

            return nodesList.AsReadOnly();
        }

        private void FindInRange(Vector location,
            KdTreeNode<TValue> node, double range, IList<TValue> valuesList, int depth)
        {
            if (node == null)
                return;

            var dimension = depth % this.dimensionality;
            var nodeLocation = this.locationGetter(node.Value);
            var distance = (nodeLocation - location).Norm();   // this is a 2=norm, i.e. a Euclidean one

            // Add current node to list if it lies within given range.
            // Current node cannot be same as search location.
            if (!fieldArithmetic.AlmostEqual(distance, fieldArithmetic.Zero) &&
                fieldComparer.Compare(distance, range) < 0)
            {
                valuesList.Add(node.Value);
            }

            // Check for nodes in sub-tree of near child.
            var nearChildNode = fieldComparer.Compare(location[dimension], nodeLocation[dimension]) < 0 ?
                node.LeftChild : node.RightChild;

            if (nearChildNode != null)
            {
                FindInRange(location, nearChildNode, range, valuesList, depth + 1);
            }

            // Check whether splitting hyperplane given by current node intersects with hypersphere of current smallest
            // distance around given location.
            if (fieldComparer.Compare(range, fieldArithmetic.Abs(fieldArithmetic.Subtract(
                nodeLocation[dimension], location[dimension]))) > 0)
            {
                // Check for nodes in sub-tree of far child.
                var farChildNode = nearChildNode == node.LeftChild ? node.RightChild : node.LeftChild;

                if (farChildNode != null)
                {
                    FindInRange(location, farChildNode, range, valuesList, depth + 1);
                }
            }
        }

        /// <summary>
        /// Finds the N values in the tree that are nearest to the specified location.
        /// </summary>
        /// <param name="location">The location for which to find the N nearest neighbors.</param>
        /// <param name="numNeighbors">N, the number of nearest neighbors to find.</param>
        /// <returns>The N values whose locations are nearest to <paramref name="location"/>.</returns>
        public C5.IPriorityQueue<TValue> FindNearestNNeighbors(Vector location, int numNeighbors)
        {
            if (location == null)
                throw new ArgumentNullException("location");

            var nodesList = new C5.IntervalHeap<TValue>(numNeighbors,
                new ValueDistanceComparer(this.locationGetter, this.dimensionality, location));
            var minBestValue = this.root.Value;
            var minBestDistance = fieldArithmetic.MaxValue;
            FindNearestNNeighbors(location, this.root, ref minBestValue, ref minBestDistance, numNeighbors,
                nodesList, 0);

            return nodesList;
        }

        private void FindNearestNNeighbors(Vector location, KdTreeNode<TValue> node,
            ref TValue maxBestValue, ref double maxBestDistance, int numNeighbors, C5.IPriorityQueue<TValue> valuesList,
            int depth)
        {
            if (node == null)
                return;

            var dimension = depth % this.dimensionality;
            var nodeLocation = this.locationGetter(node.Value);
            var distance = (nodeLocation - location).Norm();

            // Check if current node is better than maximum best node, and replace maximum node in list with it.
            // Current node cannot be same as search location.
            if (!fieldArithmetic.AlmostEqual(distance, fieldArithmetic.Zero) &&
                fieldComparer.Compare(distance, maxBestDistance) < 0)
            {
                TValue maxValue;
                if (valuesList.Count == numNeighbors)
                    maxValue = valuesList.DeleteMax();
                valuesList.Add(node.Value);

                if (valuesList.Count == numNeighbors)
                {
                    maxBestValue = valuesList.FindMax();
                    maxBestDistance = (this.locationGetter(maxBestValue) - location).Norm();
                }
            }

            // Check for best node in sub-tree of near child.
            var nearChildNode = fieldComparer.Compare(location[dimension], nodeLocation[dimension]) < 0 ?
                node.LeftChild : node.RightChild;

            if (nearChildNode != null)
            {
                FindNearestNNeighbors(location, nearChildNode, ref maxBestValue, ref maxBestDistance, numNeighbors,
                    valuesList, depth + 1);
            }

            // Check whether splitting hyperplane given by current node intersects with hypersphere of current smallest
            // distance around given location.
            if (fieldComparer.Compare(maxBestDistance, fieldArithmetic.Abs(fieldArithmetic.Subtract(
                nodeLocation[dimension], location[dimension]))) > 0)
            {
                // Check for best node in sub-tree of far child.
                var farChildValue = nearChildNode == node.LeftChild ? node.RightChild : node.LeftChild;

                if (farChildValue != null)
                {
                    FindNearestNNeighbors(location, farChildValue, ref maxBestValue, ref maxBestDistance, numNeighbors,
                        valuesList, depth + 1);
                }
            }
        }

        /// <summary>
        /// Finds the value in the tree that is nearest to the specified location.
        /// </summary>
        /// <param name="location">The location for which to find the nearest neighbor.</param>
        /// <returns>The value whose location is nearest to <paramref name="location"/>.</returns>
        public TValue FindNearestNeighbor(Vector location)
        {
            if (location == null)
                throw new ArgumentNullException("location");

            return FindNearestNeighbor(location, this.root, this.root.Value, fieldArithmetic.MaxValue, 0);
        }

        private TValue FindNearestNeighbor(Vector location,
            KdTreeNode<TValue> node, TValue bestValue, double bestDistance, int depth)
        {
            if (node == null)
                return bestValue;

            var dimension = depth % this.dimensionality;
            var nodeLocation = this.locationGetter(node.Value);
            var distance = (nodeLocation - location).Norm();

            // Check if current node is better than best node.
            // Current node cannot be same as search location.
            if (!fieldArithmetic.AlmostEqual(distance, fieldArithmetic.Zero) &&
                fieldComparer.Compare(distance, bestDistance) < 0)
            {
                bestValue = node.Value;
                bestDistance = distance;
            }

            // Check for best node in sub-tree of near child.
            var nearChildNode = fieldComparer.Compare(location[dimension], nodeLocation[dimension]) < 0 ?
                node.LeftChild : node.RightChild;

            if (nearChildNode != null)
            {
                var nearBestValue = FindNearestNeighbor(location, nearChildNode, bestValue, bestDistance, depth + 1);
                var nearBestLocation = this.locationGetter(nearBestValue);
                var nearBestDistance = (nearBestLocation - location).Norm();
                bestValue = nearBestValue;
                bestDistance = nearBestDistance;
            }

            // Check whether splitting hyperplane given by current node intersects with hypersphere of current smallest
            // distance around given location.
            if (fieldComparer.Compare(bestDistance, fieldArithmetic.Abs(fieldArithmetic.Subtract(
                nodeLocation[dimension], location[dimension]))) > 0)
            {
                // Check for best node in sub-tree of far child.
                var farChildValue = nearChildNode == node.LeftChild ? node.RightChild : node.LeftChild;

                if (farChildValue != null)
                {
                    var farBestValue = FindNearestNeighbor(location, farChildValue, bestValue, bestDistance, depth + 1);
                    var farBestLocation = this.locationGetter(farBestValue);
                    var farBestDistance = (farBestLocation - location).Norm();
                    bestValue = farBestValue;
                    bestDistance = farBestDistance;
                }
            }

            return bestValue;
        }

        /// <summary>
        /// Adds a node with the specified value to the tree.
        /// </summary>
        /// <param name="value">The value of the element to add.</param>
        /// <returns>The node that was added.</returns>
        /// <remarks>
        /// Nodes with duplicate values may be added to the tree.
        /// </remarks>
        public KdTreeNode<TValue> Add(TValue value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            return Add(value, this.root, 0);
        }

        private KdTreeNode<TValue> Add(TValue value, KdTreeNode<TValue> node, int depth)
        {
            if (node == null)
            {
                node = new KdTreeNode<TValue>(value);
            }
            else
            {
                // Check if node should be added to left or right sub-tree of current node.
                var dimension = depth % this.dimensionality;
                var comparison = fieldComparer.Compare(this.locationGetter(value)[dimension],
                    this.locationGetter(node.Value)[dimension]);

                if (comparison <= 0)
                {
                    node.LeftChild = Add(value, node.LeftChild, depth + 1);
                }
                else
                {
                    node.RightChild = Add(value, node.RightChild, depth + 1);
                }
            }

            return node;
        }

        /// <summary>
        /// Removes the node with the specified value from the tree.
        /// </summary>
        /// <param name="value">The value of the node to remove.</param>
        /// <returns>The node that was removed, or <see langword="null"/> if none was found.</returns>
        public KdTreeNode<TValue> Remove(TValue value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            return Remove(value, this.root, 0);
        }

        private KdTreeNode<TValue> Remove(TValue value, KdTreeNode<TValue> node, int depth)
        {
            if (node == null)
                return null;

            var dimension = depth % this.dimensionality;
            var valueLocation = this.locationGetter(value);
            var nodeLocation = this.locationGetter(node.Value);
            var comparison = fieldComparer.Compare(valueLocation[dimension], nodeLocation[dimension]);

            // Check if node to remove is in left sub-tree, right sub-tree, or has been found.
            if (comparison < 0)
            {
                node.LeftChild = Remove(value, node.LeftChild, depth + 1);
            }
            else if (comparison > 0)
            {
                node.RightChild = Remove(value, node.RightChild, depth + 1);
            }
            else
            {
                if (node.RightChild != null)
                {
                    node.Value = FindMinimum(node.RightChild, dimension, depth + 1);
                    node.RightChild = Remove(node.Value, node.RightChild, depth + 1);
                }
                else if (node.LeftChild != null)
                {
                    node.Value = FindMinimum(node.LeftChild, dimension, depth + 1);
                    node.RightChild = Remove(node.Value, node.LeftChild, depth + 1);
                    node.LeftChild = null;
                }
                else
                {
                    node = null;
                }
            }

            return node;
        }

        /// <summary>
        /// Removes all nodes in the tree except for the root node.
        /// </summary>
        public void Clear()
        {
            this.root.LeftChild = null;
            this.root.RightChild = null;
        }

        /// <summary>
        /// Determines whether the specified value is the value of any node in the tree.
        /// </summary>
        /// <param name="value">The value to locate in the tree.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> was found in the tree;
        /// <see langword="false"/>, otherwise. </returns>
        public bool Contains(TValue value)
        {
            return Find(value) != null;
        }

        /// <summary>
        /// Finds the node with the specified value.
        /// </summary>
        /// <param name="value">The value of the node to remove.</param>
        public KdTreeNode<TValue> Find(TValue value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            return Find(value, this.root);
        }

        private KdTreeNode<TValue> Find(TValue value, KdTreeNode<TValue> node)
        {
            if (node == null)
                return null;
            if (node.Value.Equals(value))
                return node;

            return Find(value, node.LeftChild) ?? Find(value, node.RightChild);
        }

        private TValue FindMinimum(KdTreeNode<TValue> node, int splittingDimension, int depth)
        {
            if (node == null)
                return default(TValue);

            var dimension = depth % this.dimensionality;
            if (dimension == splittingDimension)
            {
                // Find minimum value in left sub-tree.
                if (node.LeftChild == null)
                    return node.Value;
                else
                    return FindMinimum(node.LeftChild, splittingDimension, depth + 1);
            }
            else
            {
                // Find node with minimum value in sub-tree of current node.
                var nodeLocation = this.locationGetter(node.Value);
                var leftMinValue = FindMinimum(node.LeftChild, splittingDimension, depth + 1);
                var rightMinValue = FindMinimum(node.RightChild, splittingDimension, depth + 1);
                var leftMinValueBetter = leftMinValue != null &&
                    fieldComparer.Compare(this.locationGetter(leftMinValue)[splittingDimension],
                    nodeLocation[splittingDimension]) < 0;
                var rightMinValueBetter = rightMinValue != null &&
                    fieldComparer.Compare(this.locationGetter(rightMinValue)[splittingDimension],
                    nodeLocation[splittingDimension]) < 0;

                if (leftMinValueBetter && !rightMinValueBetter)
                    return leftMinValue;
                else if (rightMinValueBetter)
                    return rightMinValue;
                else
                    return node.Value;
            }
        }

        #region ICollection<TValue> Members

        bool ICollection<TValue>.IsReadOnly
        {
            get { return false; }
        }

        int ICollection<TValue>.Count
        {
            get { throw new NotSupportedException(); }
        }

        void ICollection<TValue>.Add(TValue item)
        {
            Add(item);
        }

        bool ICollection<TValue>.Remove(TValue item)
        {
            return Remove(item) != null;
        }

        /// <summary>
        /// Copies the values of all the nodes in the tree to the specified array, starting at the specified index.
        /// </summary>
        /// <param name="array">The array that is the destination of the copied elements.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        public void CopyTo(TValue[] array, int arrayIndex)
        {
            var enumerator = GetEnumerator();
            var index = arrayIndex;
            while (enumerator.MoveNext())
                array[index++] = enumerator.Current;
        }

        #endregion

        #region IEnumerable<TValue> Members

        /// <summary>
        /// Returns an enumerator that iterates through the nodes in the tree.
        /// </summary>
        /// <returns>An enumerator for the tree.</returns>
        public IEnumerator<TValue> GetEnumerator()
        {
            // Perform breadth-first search of tree, yielding every node found.
            var visitedNodes = new Stack<KdTreeNode<TValue>>();
            visitedNodes.Push(this.root);

            while (visitedNodes.Count > 0)
            {
                var node = visitedNodes.Pop();
                yield return node.Value;

                if (node.LeftChild != null)
                    visitedNodes.Push(node.LeftChild);
                if (node.RightChild != null)
                    visitedNodes.Push(node.RightChild);
            }
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<TValue>)this).GetEnumerator();
        }

        #endregion

        // Compares node values by their locations.
        private class ValueLocationComparer : Comparer<TValue>
        {
            // Function that returns location of element in kd-space.
            private Func<TValue, Vector> locationGetter;

            public ValueLocationComparer(Func<TValue, Vector> locationGetter)
            {
                Debug.Assert(locationGetter != null);
                this.locationGetter = locationGetter;
            }

            // Index of dimension of components to compare.
            public int Dimension
            {
                get;
                set;
            }

            public override int Compare(TValue x, TValue y)
            {
                return fieldComparer.Compare(
                    this.locationGetter(x)[this.Dimension],
                    this.locationGetter(y)[this.Dimension]);
            }
        }

        // Compares node values by their distances.
        private class ValueDistanceComparer : Comparer<TValue>
        {
            // Function that returns location of element in kd-space.
            public Func<TValue, Vector> locationGetter;

            // Dimensionality of kd-tree.
            private int dimensionality;

            // Location from which to calculate distance.
            private Vector location;

            public ValueDistanceComparer(Func<TValue, Vector> locationGetter, int dimensionality,
                Vector location)
            {
                Debug.Assert(locationGetter != null);
                this.locationGetter = locationGetter;
                this.dimensionality = dimensionality;
                this.location = location;
            }

            public override int Compare(TValue x, TValue y)
            {
                return fieldComparer.Compare(
                    (this.locationGetter(x) - location).Norm(),
                    (this.locationGetter(y) - location).Norm());
            }
        }
    }
}
