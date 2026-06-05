namespace UnicodeEmoji.StringProperties.Generator;

/// <summary>
/// Builds a flattened, deterministic trie keyed on Unicode scalar values from a set of emoji sequences.
/// </summary>
/// <remarks>
/// Nodes are numbered in breadth-first order with children visited in ascending key order, which makes
/// the emitted tables stable across runs (diff-friendly) regardless of input ordering. The flattened
/// form matches the layout consumed by <c>UnicodeEmoji.StringProperties.Internal.EmojiTrie</c>.
/// </remarks>
public sealed class EmojiTrieBuilder
{
    private sealed class Node
    {
        public readonly SortedDictionary<int, Node> Children = new();
        public EmojiPropertyFlags Props;
        public int Index = -1;
    }

    private readonly Node _root = new();

    /// <summary>Adds a sequence and its property to the trie.</summary>
    /// <param name="scalars">The scalar values of the sequence.</param>
    /// <param name="property">The property the sequence satisfies.</param>
    public void Add(ReadOnlySpan<int> scalars, EmojiPropertyFlags property)
    {
        Node node = _root;
        foreach (int scalar in scalars)
        {
            if (!node.Children.TryGetValue(scalar, out Node? child))
            {
                child = new Node();
                node.Children.Add(scalar, child);
            }

            node = child;
        }

        // OR the property in: distinct type fields that normalize to the same scalar sequence combine.
        node.Props |= property;
    }

    /// <summary>Adds every sequence in <paramref name="sequences"/>.</summary>
    /// <param name="sequences">The sequences to insert.</param>
    public void AddRange(IEnumerable<EmojiSequence> sequences)
    {
        foreach (EmojiSequence sequence in sequences)
        {
            Add(sequence.Scalars, sequence.Property);
        }
    }

    /// <summary>Flattens the trie into the parallel arrays consumed at runtime.</summary>
    /// <returns>The flattened trie.</returns>
    public FlatTrie Build()
    {
        // Assign node indices in breadth-first order (root == 0).
        var order = new List<Node>();
        var queue = new Queue<Node>();
        _root.Index = 0;
        order.Add(_root);
        queue.Enqueue(_root);

        while (queue.Count > 0)
        {
            Node node = queue.Dequeue();
            foreach (Node child in node.Children.Values)
            {
                child.Index = order.Count;
                order.Add(child);
                queue.Enqueue(child);
            }
        }

        int nodeCount = order.Count;
        int edgeCount = 0;
        foreach (Node node in order)
        {
            edgeCount += node.Children.Count;
        }

        var childOffsets = new int[nodeCount + 1];
        var childKeys = new int[edgeCount];
        var childNodes = new int[edgeCount];
        var props = new byte[nodeCount];

        int edge = 0;
        for (int i = 0; i < nodeCount; i++)
        {
            Node node = order[i];
            childOffsets[i] = edge;
            props[i] = (byte)node.Props;
            foreach (KeyValuePair<int, Node> entry in node.Children)
            {
                childKeys[edge] = entry.Key;
                childNodes[edge] = entry.Value.Index;
                edge++;
            }
        }

        childOffsets[nodeCount] = edge;
        return new FlatTrie(childOffsets, childKeys, childNodes, props);
    }
}

/// <summary>The flattened trie tables.</summary>
/// <param name="ChildOffsets">Per-node start offsets into <paramref name="ChildKeys"/> / <paramref name="ChildNodes"/>; length is node count + 1.</param>
/// <param name="ChildKeys">Edge scalar keys, sorted ascending within each node.</param>
/// <param name="ChildNodes">Edge target node indices.</param>
/// <param name="Props">Per-node property flags (terminal when non-zero).</param>
public sealed record FlatTrie(int[] ChildOffsets, int[] ChildKeys, int[] ChildNodes, byte[] Props)
{
    /// <summary>The number of nodes in the trie.</summary>
    public int NodeCount => Props.Length;

    /// <summary>The number of edges in the trie.</summary>
    public int EdgeCount => ChildKeys.Length;
}
