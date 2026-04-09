using System;
using System.Collections.Generic;
using System.Text;

namespace AssetHub.Shared.Service.Transformation {
    public class PayloadValidationException: Exception {
        public IReadOnlyList<ValidationError> Errors { get; }

        public PayloadValidationException(IEnumerable<ValidationError> errors)
            : base(BuildMessage(errors)) {
            Errors = errors.ToList();
        }

        private static string BuildMessage(IEnumerable<ValidationError> errors)
            => "Payload validation failed: " + string.Join("; ", errors.Select(e => $"{e.Field}: {e.Message}"));
    }
}
