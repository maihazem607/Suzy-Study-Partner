using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Suzy.Models
{
    public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public ICollection<NoteCategory> NoteCategories { get; set; } = new List<NoteCategory>();
}

}
