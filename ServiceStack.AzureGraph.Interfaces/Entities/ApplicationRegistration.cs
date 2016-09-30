using ServiceStack.DataAnnotations;
using ServiceStack.Model;

namespace ServiceStack.AzureGraph.ServiceModel.Entities
{
    public class ApplicationRegistration : IHasLongId
    {
        [AutoIncrement]
        public long Id { get; set; }

        [Required]
        [StringLength(38)]
        public string ClientId { get; set; }

        [Required]
        [StringLength(64)]
        public string ClientSecret { get; set; }

        [Required]
        public string DirectoryName { get; set; }
        public ulong RowVersion { get; set; }

        public long? RefId { get; set; }

        [StringLength(128)]
        public string RefIdStr { get; set; }
    }
}