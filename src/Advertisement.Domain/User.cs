using Advertisement.Domain.Shared;

namespace Advertisement.Domain
{
    public sealed class User : MutableEntity<string>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
    }
}