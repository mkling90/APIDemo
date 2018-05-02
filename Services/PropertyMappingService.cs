using Library.API.Entities;
using Library.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Services
{

    public interface IPropertyMappingService
    {
        Dictionary<string, PropertyMappingValue> GetPropertyMapping<TSource, TDestination>();
        bool ValidMappingExistsFor<TSource, TDestination>(string fields);
    }

    public class PropertyMappingService : IPropertyMappingService
    {
        //map string from dto to the entity property
        private Dictionary<string, PropertyMappingValue> _authorPropertyMapping =
            new Dictionary<string, PropertyMappingValue>(StringComparer.OrdinalIgnoreCase)
            {
                {"Id", new PropertyMappingValue(new List<string>(){"Id"}) },
                {"Genre", new PropertyMappingValue(new List<string>(){"Genre"}) },
                {"Age", new PropertyMappingValue(new List<string>(){"DateOfBirth"}, true) },  //age reverses date of birth order
                {"Name", new PropertyMappingValue(new List<string>(){"FirstName", "LastName"}) },
            };

        //need the empty interface to be able to resolve TSource, TDestination.  this way we can declare the interface, not the class
        //private IList<PropertyMapping<TSource, TDestination>> propertyMappings;
        private IList<IPropertyMapping> propertyMappings = new List<IPropertyMapping>() ;

        public PropertyMappingService()
        {
            propertyMappings.Add(new PropertyMapping<AuthorDto, Author>(_authorPropertyMapping));
        }

        //need method to ask for a property mapping
        public Dictionary<string, PropertyMappingValue> GetPropertyMapping <TSource, TDestination>()
        {
            //find matching mapping based on the specific type needed
            var matchingMapping = propertyMappings.OfType<PropertyMapping<TSource, TDestination>>();
            if (matchingMapping.Count() == 1)
                return matchingMapping.First()._mappingDictionary;

            throw new Exception($"cannot find requested mapping for {typeof(TSource)} ");
            
        }

        public bool ValidMappingExistsFor<TSource, TDestination>(string fields)
        {
            var propertyMapping = GetPropertyMapping<TSource, TDestination>();
            if (String.IsNullOrWhiteSpace(fields))
                return true;

            //fields come in as a cdl
            var fieldsAfterSplit = fields.Split(',');
            
            //check each field
            foreach(string field in fieldsAfterSplit)
            {
                string trimmedField = field.Trim().ToLowerInvariant();
                //remove everything after the first space - if thats the orderBy part, should be ignored in field check
                var indexOfSpace = trimmedField.IndexOf(" ");
                var propertyName = indexOfSpace == -1 ?
                    trimmedField : trimmedField.Remove(indexOfSpace);

                //find match
                if (!propertyMapping.ContainsKey(propertyName))
                    return false;
            }
            return true;
        }
    }
}
