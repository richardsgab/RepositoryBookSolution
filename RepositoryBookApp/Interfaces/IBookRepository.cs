using RepositoryBookApp.Models.DomeinModels;

namespace RepositoryBookApp.Interfaces
{
    public interface IBookRepository:IRepository<Book>
    {
        Task<(IEnumerable<Book> Books, int Count)> GetAllBooksWithAuthorsAndgenresAsync(int pageNumber, int pageSize);
        Task<Book> GetBookWithGenresAsync(int id);
        Task<Book> GetBookWithGenresAndAuthorsAsync(int id);
    }
}
