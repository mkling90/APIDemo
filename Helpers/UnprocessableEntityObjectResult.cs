using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Helpers
{
    public class UnprocessableEntityObjectResult: ObjectResult
    {
        /*
         * The 422 (Unprocessable Entity) status code means the server understands the content type of the request entity
           (hence a 415(Unsupported Media Type) status code is inappropriate), 
           and the syntax of the request entity is correct(thus a 400 (Bad Request) status code is inappropriate) 
           but was unable to process the contained instructions.
          */
        public UnprocessableEntityObjectResult(ModelStateDictionary modelState) : base(new SerializableError(modelState))
        {
            //SerializableError = Serializable container for key/value pairs
            if (modelState == null)
                throw new ArgumentNullException(nameof(modelState));

            StatusCode = 422;  //422 status code
        }

    }
}
