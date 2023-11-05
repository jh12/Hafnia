namespace Hafnia.DataAccess.DataStructures;

public class Tree<T>
{
    private readonly LinkedList<TreeNode<T>> _children = new();

    public void AddChild(TreeNode<T> subTreeNode)
    {
        _children.AddFirst(subTreeNode);
    }

    public IEnumerable<TreeNode<T>> GetChildren()
    {
        return _children;
    }

    public IEnumerable<T> GetDataFromDescendants(Func<T, bool> branchFilter)
    {
        List<TreeNode<T>> descendants = _children.SelectMany(c => c.BreathFirstSearch()).ToList();

        List<TreeNode<T>> branches = descendants.Where(d => branchFilter(d.Data)).ToList();

        return branches.SelectMany(b => b.BreathFirstSearch().Select(t => t.Data)).ToList();
    }
}
