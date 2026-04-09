using AssetHub.Shared.Interface;
using AssetHub.Shared.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace AssetHub.Shared.Service.Transformation {
    public class FieldOpsAssetPayloadTransformer : IFieldOpsAssetPayloadTransformer {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) {
            PropertyNameCaseInsensitive = true
        };
        public TransformationResult<AssetUpsertPayload> Transform(string rawJson) {
            var result = new TransformationResult<AssetUpsertPayload>();

            if (string.IsNullOrWhiteSpace(rawJson)) {
                result.Errors.Add(new ValidationError("rawJson", "Raw event body is required."));
                return result;
            }

            FieldOpsEnvelope? envelope;
            try {
                envelope = JsonSerializer.Deserialize<FieldOpsEnvelope>(rawJson, JsonOptions);
            } catch (JsonException ex) {
                result.Errors.Add(new ValidationError("rawJson", $"Invalid JSON. {ex.Message}"));
                return result;
            }

            if (envelope is null) {
                result.Errors.Add(new ValidationError("rawJson", "Event body could not be deserialized."));
                return result;
            }

            var eventType = NormalizeRequired(envelope.EventType, "eventType", result.Errors);
            var eventId = NormalizeRequired(envelope.EventId, "eventId", result.Errors);
            var projectId = NormalizeRequired(envelope.ProjectId, "projectId", result.Errors);
            var siteRef = NormalizeOptional(envelope.SiteRef);

            if (eventType is null || eventId is null || projectId is null) {
                return result;
            }

            AssetUpsertPayload? payload = eventType switch {
                "asset.registration.submitted" => TransformRegistration(envelope, eventId, projectId, siteRef, result.Errors),
                "asset.checkin.updated" => TransformCheckin(envelope, eventId, projectId, siteRef, result.Errors),
                _ => InvalidEventType(eventType, result.Errors)
            };

            if (payload is not null && result.Errors.Count == 0) {
                result.Payload = payload;
            }

            return result;
        }

        public AssetUpsertPayload TransformOrThrow(string rawJson) {
            var result = Transform(rawJson);
            if (!result.IsValid) {
                throw new PayloadValidationException(result.Errors);
            }

            return result.Payload!;
        }

        private static AssetUpsertPayload? TransformRegistration(
        FieldOpsEnvelope envelope,
        string eventId,
        string projectId,
        string? siteRef,
        List<ValidationError> errors) {
            if (envelope.Fields is null) {
                errors.Add(new ValidationError("fields", "fields object is required for asset.registration.submitted."));
                return null;
            }

            var assetName = NormalizeRequired(envelope.Fields.AssetName, "fields.assetName", errors);
            var make = NormalizeRequired(envelope.Fields.Make, "fields.make", errors);
            var model = NormalizeRequired(envelope.Fields.Model, "fields.model", errors);
            var serialNumber = NormalizeRequired(envelope.Fields.SerialNumber, "fields.serialNumber", errors);

            var category = NormalizeOptional(envelope.Fields.Category);
            var type = NormalizeOptional(envelope.Fields.Type);
            var supplier = NormalizeOptional(envelope.Fields.Supplier);
            var imageUrl = NormalizeOptional(envelope.ImageUrl);

            int? yearMfg = ParseOptionalInt(envelope.Fields.YearMfg, "fields.yearMfg", errors);
            decimal? ratePerHour = ParseOptionalDecimal(envelope.Fields.RatePerHour, "fields.ratePerHour", errors);

            if (assetName is null || make is null || model is null || serialNumber is null) {
                return null;
            }

            return new AssetUpsertPayload {
                ProjectId = projectId,
                EventId = eventId,
                EventType = "asset.registration.submitted",

                AssetId = BuildAssetId(make, model, serialNumber),
                AssetName = assetName,
                Ownership = "Subcontracted",

                Make = make,
                Model = model,
                SerialNumber = serialNumber,

                SiteReference = siteRef,
                Category = category,
                Type = type,
                YearManufactured = yearMfg,
                RatePerHour = ratePerHour,
                Supplier = supplier,
                ImageUrl = imageUrl,

                CheckInDate = null,
                CheckOutDate = null,
                Onsite = null
            };
        }

        private static AssetUpsertPayload? TransformCheckin(
            FieldOpsEnvelope envelope,
            string eventId,
            string projectId,
            string? siteRef,
            List<ValidationError> errors) {
            var make = NormalizeRequired(envelope.Make, "make", errors);
            var model = NormalizeRequired(envelope.Model, "model", errors);
            var serialNumber = NormalizeRequired(envelope.SerialNumber, "serialNumber", errors);

            var checkInDate = ParseRequiredDateTimeOffset(envelope.CheckInDate, "checkInDate", errors);
            var checkOutDate = ParseOptionalDateTimeOffset(envelope.CheckOutDate, "checkOutDate", errors);

            if (make is null || model is null || serialNumber is null || checkInDate is null) {
                return null;
            }

            bool onsite = checkOutDate is null;

            return new AssetUpsertPayload {
                ProjectId = projectId,
                EventId = eventId,
                EventType = "asset.checkin.updated",

                AssetId = BuildAssetId(make, model, serialNumber),
                AssetName = BuildFallbackAssetName(make, model, serialNumber),
                Ownership = "Subcontracted",

                Make = make,
                Model = model,
                SerialNumber = serialNumber,

                SiteReference = siteRef,
                Category = null,
                Type = null,
                YearManufactured = null,
                RatePerHour = null,
                Supplier = null,
                ImageUrl = null,

                CheckInDate = checkInDate,
                CheckOutDate = checkOutDate,
                Onsite = onsite
            };
        }

        private static AssetUpsertPayload? InvalidEventType(string eventType, List<ValidationError> errors) {
            errors.Add(new ValidationError("eventType", $"Unsupported event type '{eventType}'."));
            return null;
        }

        private static string BuildAssetId(string make, string model, string serialNumber)
            => $"{make}-{model}-{serialNumber}";

        private static string BuildFallbackAssetName(string make, string model, string serialNumber)
            => $"{make} {model} {serialNumber}";

        private static string? NormalizeRequired(string? value, string field, List<ValidationError> errors) {
            var normalized = NormalizeOptional(value);
            if (normalized is null) {
                errors.Add(new ValidationError(field, "Value is required."));
            }

            return normalized;
        }

        private static string? NormalizeOptional(string? value) {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim();
            return CollapseWhitespace(trimmed);
        }

        private static string CollapseWhitespace(string value) {
            var buffer = new List<char>(value.Length);
            bool previousWasWhitespace = false;

            foreach (var ch in value) {
                if (char.IsWhiteSpace(ch)) {
                    if (!previousWasWhitespace) {
                        buffer.Add(' ');
                        previousWasWhitespace = true;
                    }
                } else {
                    buffer.Add(ch);
                    previousWasWhitespace = false;
                }
            }

            return new string(buffer.ToArray());
        }

        private static int? ParseOptionalInt(string? value, string field, List<ValidationError> errors) {
            var normalized = NormalizeOptional(value);
            if (normalized is null)
                return null;

            if (int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                return parsed;

            errors.Add(new ValidationError(field, $"Value '{normalized}' is not a valid integer."));
            return null;
        }

        private static decimal? ParseOptionalDecimal(string? value, string field, List<ValidationError> errors) {
            var normalized = NormalizeOptional(value);
            if (normalized is null)
                return null;

            if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
                return parsed;

            errors.Add(new ValidationError(field, $"Value '{normalized}' is not a valid decimal."));
            return null;
        }

        private static DateTimeOffset? ParseRequiredDateTimeOffset(string? value, string field, List<ValidationError> errors) {
            var parsed = ParseOptionalDateTimeOffset(value, field, errors);
            if (parsed is null) {
                errors.Add(new ValidationError(field, "Value is required."));
            }

            return parsed;
        }

        private static DateTimeOffset? ParseOptionalDateTimeOffset(string? value, string field, List<ValidationError> errors) {
            var normalized = NormalizeOptional(value);
            if (normalized is null)
                return null;

            if (DateTimeOffset.TryParse(normalized, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
                return parsed;

            errors.Add(new ValidationError(field, $"Value '{normalized}' is not a valid ISO-8601 date/time."));
            return null;
        }
    }
}
