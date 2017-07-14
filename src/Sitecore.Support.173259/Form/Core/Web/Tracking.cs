namespace Sitecore.Support.Form.Core.Web
{
  using Sitecore.Form.Core.Analytics;
  using Sitecore.Form.Core.Web;

  public class Tracking : System.Web.UI.Page
  {
    protected void Page_Init(object sender, System.EventArgs e)
    {
      string value = this.Page.Server.UrlDecode(this.Page.Request.Form["track"]);
      if (!string.IsNullOrEmpty(value))
      {
        ClientEvent[] array = Json.Instance.DeserializeObject<ClientEvent[]>(value);
        ClientEvent[] array2 = array;
        for (int i = 0; i < array2.Length; i++)
        {
          ClientEvent clientEvent = array2[i];
          if (clientEvent.Value == "schidden")
          {
            clientEvent.Value = "<schidden></schidden>";
          }
          AnalyticsTracker.TriggerEvent(clientEvent, true);
        }
      }
      base.Response.End();
    }
  }
}