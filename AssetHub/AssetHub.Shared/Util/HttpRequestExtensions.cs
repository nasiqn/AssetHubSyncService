using System;
using System.Collections.Generic;
using System.Text;

namespace AssetHub.Shared.Util {
    public static class HttpRequestExtensions {
        public static async Task<HttpRequestMessage> CloneAsync(this HttpRequestMessage request, CancellationToken cancellationToken = default) {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);

            foreach (var header in request.Headers) {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            foreach (var option in request.Options) {
                clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
            }

            if (request.Content is not null) {
                var ms = new MemoryStream();
                await request.Content.CopyToAsync(ms, cancellationToken);
                ms.Position = 0;

                var newContent = new StreamContent(ms);

                foreach (var header in request.Content.Headers) {
                    newContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                clone.Content = newContent;
            }

            clone.Version = request.Version;
            clone.VersionPolicy = request.VersionPolicy;

            return clone;
        }
    }
}
