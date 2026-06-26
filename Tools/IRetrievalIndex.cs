namespace SupportAgent.Tools;

public interface IRetrievalIndex
{
    IReadOnlyList<SearchHit> Search(string query, int maxResults = 5);
}
