using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
namespace HelloWorldPlusApi.Controllers;   
[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private static readonly List<Book> books = new();
    private static int nextId = 1;

    /// <summary>
    /// Retrieves all books in the system.
    /// </summary>
    /// <returns>A list of books.</returns>
    /// <response code="200">Returns the list of books.</response>
    [HttpGet]
    public ActionResult<IEnumerable<Book>> GetBooks()
    {
        return Ok(books);
    }

    /// <summary>
    /// Retrieves a book by its ID.
    /// </summary>
    /// <param name="id">The ID of the book to retrieve.</param>
    /// <returns>The requested book.</returns>
    /// <response code="200">Returns the requested book.</response>
    /// <response code="404">If the book is not found.</response>
    [HttpGet("{id}")]
    public ActionResult<Book> GetById(int id)
    {
        var book = books.FirstOrDefault(b => b.Id == id);
        if (book == null)
            return NotFound($"Book with ID {id} not found.");
        return Ok(book);
    }

    /// <summary>
    /// Creates a new book.
    /// </summary>
    /// <param name="dto">The book data to create.</param>
    /// <returns>The created book.</returns>
    /// <response code="201">Returns the newly created book.</response>
    /// <response code="400">If the input data is invalid.</response>
    [HttpPost]
    public ActionResult<Book> CreateBook([FromBody] BookCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (books.Any(b => b.ISBN == dto.ISBN))
            return BadRequest("ISBN must be unique.");

        var book = new Book
        {
            Id = nextId++,
            Title = dto.Title,
            Author = dto.Author,
            PublishedYear = dto.PublishedYear,
            ISBN = dto.ISBN
        };

        books.Add(book);
        return CreatedAtAction(nameof(GetById), new { id = book.Id }, book);
    }

    /// <summary>
    /// Updates an existing book.
    /// </summary>
    /// <param name="id">The ID of the book to update.</param>
    /// <param name="dto">The updated book data.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Book updated successfully.</response>
    /// <response code="400">If the input data is invalid.</response>
    /// <response code="404">If the book is not found.</response>
    [HttpPut("{id}")]
    public IActionResult UpdateBook(int id, [FromBody] BookCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var book = books.FirstOrDefault(b => b.Id == id);
        if (book == null)
            return NotFound($"Book with ID {id} not found.");

        book.Title = dto.Title;
        book.Author = dto.Author;
        book.PublishedYear = dto.PublishedYear;
        book.ISBN = dto.ISBN;

        return NoContent();
    }

    /// <summary>
    /// Deletes a book by its ID.
    /// </summary>
    /// <param name="id">The ID of the book to delete.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Book deleted successfully.</response>
    /// <response code="404">If the book is not found.</response>
    [HttpDelete("{id}")]
    public IActionResult DeleteBook(int id)
    {
        var book = books.FirstOrDefault(b => b.Id == id);
        if (book == null)
            return NotFound($"Book with ID {id} not found.");

        books.Remove(book);
        return NoContent();
    }
}

//Book model
/// <summary>
/// Represents a book entity with basic metadata.
/// </summary>
public class Book
{
    /// <summary>
    /// Unique identifier for the book.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Title of the book.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Title { get; set; } = default!;

    /// <summary>
    /// Author of the book.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Author { get; set; } = default!;

    /// <summary>
    /// Year the book was published.
    /// </summary>
    [Range(1000, 9999)]
    public int PublishedYear { get; set; }

    /// <summary>
    /// International Standard Book Number.
    /// </summary>
    [Required]
    [StringLength(13)]
    public string ISBN { get; set; } = default!;
}

//Data transfer object for book creation
/// <summary>
/// Data Transfer Object for creating a new book.
/// </summary>
public class BookCreateDto
{
    /// <summary>
    /// Title of the book.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Title { get; set; } = default!;

    /// <summary>
    /// Author of the book.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Author { get; set; } = default!;

    /// <summary>
    /// Year the book was published.
    /// </summary>
    [Range(1000, 9999)]
    public int PublishedYear { get; set; }

    /// <summary>
    /// International Standard Book Number.
    /// </summary>
    [Required]
    [StringLength(13)]
    public string ISBN { get; set; } = default!;
}