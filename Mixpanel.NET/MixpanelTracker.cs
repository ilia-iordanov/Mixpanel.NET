using System;
using System.Collections.Generic;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Linq;

namespace Mixpanel.NET {
  public class MixpanelTracker : IEventTracker
  {
    readonly IMixpanelHttp _http;
    readonly TrackerOptions _options;
    readonly string _token;

    public MixpanelTracker(string token, TrackerOptions options = null) : this(token, new MixpanelHttp(), options) { }

    /// <summary>
    /// Creates a new Mixpanel tracker for a given API token
    /// </summary>
    /// <param name="token">The API token for MixPanel</param>
    /// <param name="http">An implementation of IMixpanelHttp, <see cref="MixpanelHttp"/>
    /// Determines if class names and properties will be serialized to JSON literally.
    /// If false (the default) spaces will be inserted between camel-cased words for improved 
    /// readability on the reporting side.
    /// </param>
    public MixpanelTracker(string token, IMixpanelHttp http, TrackerOptions options = null) {
      _token = token;
      _http = http;
      _options = options ?? new TrackerOptions();
    }

    public bool Track(string @event, IDictionary<string, object> properties) {
      properties["token"] = _token;
      if (!properties.ContainsKey("Time") || !properties.ContainsKey("time"))
        properties["time"] = DateTime.UtcNow;
      if ((!properties.ContainsKey("Bucket") || !properties.ContainsKey("bucket")) && _options.Bucket != null)
        properties["bucket"] = _options.Bucket;
      var data = new JavaScriptSerializer().Serialize(new Dictionary<string, object> {
        {"event", @event}, {"properties", properties}
      });
      var values = "data=" + data.Base64Encode();
      if (_options.Test) values += "&test=1";
      var contents = _options.UseGet 
        ? _http.Get(Resources.Track(_options.ProxyUrl), values)
        : _http.Post(Resources.Track(_options.ProxyUrl), values);
      return contents == "1";
    }    

    public bool Track(MixpanelEvent @event) {
      return Track(@event.Event, @event.Properties);
    }

    public bool Track<T>(T @event) {
      return Track(@event.ToMixpanelEvent(_options.LiteralSerialization));
    }
  }
}