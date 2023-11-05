namespace Hafnia.DataAccess.DataStructures;

public class TreeNode<T>
{
    public readonly T Data;
    private readonly LinkedList<TreeNode<T>> _children;

    public TreeNode(T data)
    {
        Data = data;
        _children = new LinkedList<TreeNode<T>>();
    }

    public void AddChild(TreeNode<T> subTreeNode)
    {
        _children.AddFirst(subTreeNode);
    }

    public IEnumerable<TreeNode<T>> BreathFirstSearch()
    {
        Queue<TreeNode<T>> queue = new Queue<TreeNode<T>>();
        queue.Enqueue(this);

        while (queue.Count > 0)
        {
            TreeNode<T> current = queue.Dequeue();
            yield return current;

            foreach (TreeNode<T> child in current._children)
            {
                queue.Enqueue(child);
            }
        }
    }

    public override string ToString()
    {
        return $"{Data} - Children: {_children.Count}";
    }
}
