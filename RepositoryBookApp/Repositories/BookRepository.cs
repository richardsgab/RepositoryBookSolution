﻿using Microsoft.EntityFrameworkCore;
using RepositoryBookApp.Data;
using RepositoryBookApp.Interfaces;
using RepositoryBookApp.Models.DomeinModels;

namespace RepositoryBookApp.Repositories
{
    public class BookRepository : Repository<Book>, IBookRepository
    {
        private readonly RepoContext _context;
        public BookRepository(RepoContext context) : base(context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<Book> Books, int Count)> GetAllBooksWithAuthorsAndgenresAsync(int pageNumber, int pageSize)
        {
            var count = await _context.Books.CountAsync();
            var books = await _context.Books
                .Include(b => b.Author)
                    .Include(b => b.BookGenres)
                    .ThenInclude(bg => bg.Genre)
                    .OrderByDescending(b => b.BookId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (books, count);
        }

        public async Task<Book> GetBookWithGenresAndAuthorsAsync(int id)
        {
            var boek = await _context.Books
                 .Include(b => b.Author)
                 .Include(b => b.BookGenres)
                     .ThenInclude(bg => bg.Genre)
                 .FirstOrDefaultAsync(b => b.BookId == id);
            return boek;
        }

        public async Task<Book> GetBookWithGenresAsync(int id)
        {
            return await _context.Books
                .Include(b => b.BookGenres)
                .FirstOrDefaultAsync(b => b.BookId == id);
        }
    }
}
