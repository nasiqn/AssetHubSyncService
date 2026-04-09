using System;
using System.Collections.Generic;
using System.Text;

namespace AssetHub.Shared.Service.Transformation {
    public class TransformationResult<T> {
        public bool IsValid => Errors.Count == 0;
        public T? Payload { get; set; }
        public List<ValidationError> Errors { get; } = new();
    }

    public sealed record ValidationError(string Field, string Message);
}
