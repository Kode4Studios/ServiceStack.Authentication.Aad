using System;
using ServiceStack.DataAnnotations;
using ServiceStack.Model;

namespace ServiceStack.Authentication.Aad
{
    public class DirectoryRegistration : IHasLongId
    {
        private string _directoryDomain;

        [Required]
        [StringLength(128)]
        public string ClientId { get; set; }

        [Required]
        [StringLength(128)]
        public string ClientSecret { get; set; }

        [Required]
        [StringLength(40)]
        [Index(Unique = true)]
        public string TenantId { get; set; }

        [Required]
        [StringLength(128)]
        public string DirectoryDomain
        {
            get { return _directoryDomain; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _directoryDomain = null;
                    DomainHint = null;
                }
                else
                {
                    if (!value.StartsWith("@"))
                        throw new ArgumentException("DirectoryDomain value must start with @ symbol.");
                    _directoryDomain = value.ToLower();
                    DomainHint = value.Substring(1);
                }
            }
        }

        public ulong RowVersion { get; set; }

        [StringLength(128)]
        public string DomainHint { get; set; }

        public long? RefId { get; set; }

        [StringLength(128)]
        public string RefIdStr { get; set; }

        [AutoIncrement]
        public long Id { get; set; }
    }
}