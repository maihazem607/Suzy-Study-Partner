using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Suzy.Models
{
    // Enum to define priority levels for a task
    public enum PriorityLevel
    {
        Low,
        Medium,
        High
    }

    // This is a new model, separate from your existing TodoItem
    public class TaskItem
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        // We'll use a status enum for the Kanban board
        public TaskStatus Status { get; set; } = TaskStatus.ToDo;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? DueDate { get; set; }

        public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;

        [Required]
        public string UserId { get; set; }
        public IdentityUser User { get; set; }
    }

    // Enum to manage the Kanban board columns
    public enum TaskStatus
    {
        ToDo,
        InProgress,
        Completed
    }
}