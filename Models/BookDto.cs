using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Models
{
    //inherit the linked resource dto so we can add the collection of links
    public class BookDto : LinkedResourceBaseDto
    {
        // Data Annotations are the default validation rules
        
        public Guid Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public Guid AuthorId { get; set; }
    }
    // abstract, this class must be derived from for each create/update implementation
    public abstract class BookForManipulationDto
    {
        [Required(ErrorMessage = "Please provide a Title")]
        [MaxLength(100)]
        public string Title { get; set; }

        //virtual to allow overriding
        [MaxLength(500, ErrorMessage = "Description should be less then 500 characters")]
        public virtual string Description { get; set; }
    }

    public class BookForCreationDto : BookForManipulationDto
    { //identical fields to base    
    }

    //  Use a different dto for update - frequently the fields could be different, different validation, etc.
    public class BookForUpdateDto : BookForManipulationDto
    {
        [Required(ErrorMessage = "Please provide a Description")]
        public override string Description { get; set; }
    }
}
