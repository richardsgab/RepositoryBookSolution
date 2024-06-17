﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using RepositoryBookApp.Data;
using RepositoryBookApp.Helper;
using RepositoryBookApp.Interfaces;
using RepositoryBookApp.Models.DomeinModels;
using RepositoryBookApp.Models.ViewModels;

namespace RepositoryBookApp.Controllers
{
    public class BooksController : Controller
    {
        private readonly IUnitOfWork _unitOfWork; //en vez de llamar al context, porque esta dentro de IUnitOfWork
        private readonly IWebHostEnvironment _webHostEnvironment;//files uploaden
        private readonly RepoContext _context;

        public BooksController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index(int pagenumber = 1, int pagesize = 3)
        {
            if (Request.Cookies["PageSize"] != null && int.TryParse(Request.Cookies["PageSize"], out var storedPageSize))
            {
                pagesize = storedPageSize;
            }

            var (books, count) = await _unitOfWork.BooksRelated.GetAllBooksWithAuthorsAndgenresAsync(pagenumber, pagesize);

            var booksViewModels = books.Select(book => new BookIndexViewModel
            {
                BookId = book.BookId,
                Title = book.Title,
                AuthorName = book.Author.Name,
                GenreNames = book.BookGenres.Select(bg => bg.Genre.Name).ToList(),
                IsAvailable = book.IsAvailable,
                IsNewRelease = book.IsNewRelease,
                IsBestSeller = book.IsBestSeller,
                BindingType = book.BindingType.ToString()
            }).ToList();

            var paginatedbooks = new PaginatedList<BookIndexViewModel>(booksViewModels, count, pagenumber, pagesize);
            var viewModel = new BookListViewModel
            {
                Books = paginatedbooks,
                TotalBooks = count
            };
            ViewBag.PageSize = pagesize;
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetPageSize(int pageSize) //Cookies instellingen
        {
            if (pageSize < 3 || pageSize > 20)
            {
                ModelState.AddModelError("PageSize", "The page size must be between 3 and 20");
                ViewBag["PageSize"] = pageSize;
                return View("Index", new BookListViewModel { Books = new PaginatedList<BookIndexViewModel>(new List<BookIndexViewModel>(), 0, 1, pageSize) });
            }

            CookieOptions options = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(10)
            };
            Response.Cookies.Append("PageSize", pageSize.ToString(), options);
            return RedirectToAction(nameof(Index));
        }


        [HttpGet]//Create method with UnitOfWork
        public async Task<IActionResult> Create()
        {
            //First, een model creeren(VM)
            var viewModel = new CreateBookViewModel
            {
                Authors = (await _unitOfWork.Authors.GetAllAsync()).ToList(),
                Genres = (await _unitOfWork.Genres.GetAllAsync()).ToList(),
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBookViewModel viewModel)
        {
            if (!ModelState.IsValid)//show something in case of data non valable
            {
                viewModel.Authors = (await _unitOfWork.Authors.GetAllAsync()).ToList();
                viewModel.Genres = (await _unitOfWork.Genres.GetAllAsync()).ToList();
                return View(viewModel);
            };

            /*string imagePath = await SaveImageAsync(viewModel.Image);*/
            string? imagePath = viewModel.Image != null && viewModel.Image.Length > 0
            ? await SaveImageAsync(viewModel.Image)
            :   "/images/Default.jpg";

            var newBook = new Book
            {
                Title = viewModel.Books.Title,
                AuthorId = viewModel.SelectedAuthorId,
                IsAvailable = viewModel.Books.IsAvailable,
                IsNewRelease = viewModel.Books.IsNewRelease,
                IsBestSeller = viewModel.Books.IsBestSeller,
                BindingType = viewModel.Books.BindingType,
                ImagePath = imagePath
            };
            await _unitOfWork.Books.AddAsync(newBook);
            await _unitOfWork.CompleteAsync();

            if (viewModel.SelectedGenres != null && viewModel.SelectedGenres.Any())
            {
                foreach (var genreId in viewModel.SelectedGenres)
                {
                    var bookGenre = new BookGenre
                    {
                        BookId = newBook.BookId,
                        GenreId = genreId
                    };
                    await _unitOfWork.BookGenres.AddAsync(bookGenre);
                }
                await _unitOfWork.CompleteAsync();              
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SaveImageAsync(IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                return null;
            }

            string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
            string filePath = Path.Combine(uploadFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            return "/images/" + uniqueFileName;
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var book = await _unitOfWork.BooksRelated.GetBookWithGenresAsync(id);
            if (book == null)
            {
                return NotFound();
            }
            var bookViewModel = new EditBookViewModel
            {
                BookId = book.BookId,
                Title = book.Title,
                AuthorId = book.AuthorId,
                SelectedGenres = book.BookGenres.Select(bg => bg.GenreId).ToList(),
                IsAvailable = book.IsAvailable,
                IsNewRelease = book.IsNewRelease,
                IsBestSeller = book.IsBestSeller,
                BindingType = book.BindingType,
                ImagePath = book.ImagePath,
                Authors = (await _unitOfWork.Authors.GetAllAsync()).ToList(),
                Genres = (await _unitOfWork.Genres.GetAllAsync()).ToList()
            };
            return View(bookViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditBookViewModel bookViewModel)
        {

            if (id != bookViewModel.BookId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var book = await _unitOfWork.BooksRelated.GetBookWithGenresAsync(id);

                if (book == null)
                {
                    return NotFound();
                }

                string? imagePath = book.ImagePath;
                if (bookViewModel.Image != null && bookViewModel.Image.Length > 0)
                {
                    imagePath = await SaveImageAsync(bookViewModel.Image);
                }

                // update book
                book.Title = bookViewModel.Title;
                book.AuthorId = bookViewModel.AuthorId;
                book.IsAvailable = bookViewModel.IsAvailable;
                book.IsNewRelease = bookViewModel.IsNewRelease;
                book.IsBestSeller = bookViewModel.IsBestSeller;
                book.BindingType = bookViewModel.BindingType;
                book.ImagePath = imagePath;

                // update genres

                book.BookGenres.Clear();
                if (bookViewModel.SelectedGenres != null)
                {
                    foreach (var genreId in bookViewModel.SelectedGenres)
                    {
                        book.BookGenres.Add(new BookGenre { BookId = book.BookId, GenreId = genreId });
                    }
                }

                await _unitOfWork.Books.UpdateAsync(book);
                await _unitOfWork.CompleteAsync();

                return RedirectToAction(nameof(Index));
            }
            else
            {
                bookViewModel.Authors = (await _unitOfWork.Authors.GetAllAsync()).ToList();
                bookViewModel.Genres = (await _unitOfWork.Genres.GetAllAsync()).ToList();
            }

            return View(bookViewModel);
        }

        public async Task<IActionResult> Details(int id)
        {            
            var book = await _unitOfWork.BooksRelated.GetBookWithGenresAndAuthorsAsync(id);
            if(book == null) 
            {
                return NotFound();
            }

            var viewModel = new BookDetailsViewModel
            {
                BookId = book.BookId,
                Title = book.Title,
                AuthorName = book.Author.Name,
                GenreNames = book.BookGenres.Select(bg => bg.Genre.Name).ToList(),
                IsAvailable = book.IsAvailable,
                IsNewRelease = book.IsNewRelease,
                IsBestSeller = book.IsBestSeller,
                BindingType = book.BindingType.ToString(),
                ImagePath = book.ImagePath
            };
            return View(viewModel);
        }
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) 
            {
                return NotFound();
            }
            var book = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.BookGenres)
                .ThenInclude(bg => bg.Genre)
                .FirstOrDefaultAsync(b =>b.BookId == id);
            if(book == null)
            { return NotFound(); }
            var viewModel = new BookDetailsViewModel
            {
                BookId = book.BookId,
                Title = book.Title,
                AuthorName = book.Author.Name,
                IsAvailable = book.IsAvailable,
                IsNewRelease = book.IsNewRelease,
                IsBestSeller = book.IsBestSeller,
                BindingType = book.BindingType?.ToString(),
                ImagePath = book.ImagePath
            };
            return View(viewModel);
        }
    }





}
