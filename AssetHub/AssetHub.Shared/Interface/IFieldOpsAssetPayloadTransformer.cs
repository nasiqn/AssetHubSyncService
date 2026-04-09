using AssetHub.Shared.Models;
using AssetHub.Shared.Service.Transformation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AssetHub.Shared.Interface {
    public interface IFieldOpsAssetPayloadTransformer {
        TransformationResult<AssetUpsertPayload> Transform(string rawJson);
        AssetUpsertPayload TransformOrThrow(string rawJson);
    }
}
