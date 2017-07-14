namespace Sitecore.Support.Form.Core.Analytics
{
  using System.Linq;
  using Sitecore.Analytics;
  using Sitecore.Analytics.Data.DataAccess.DataSets;
  using Sitecore.Form.Core.Analytics;
  using Sitecore.Form.Core.Configuration;
  using Sitecore.Form.Core.Web;

  public class  AnalyticsTracker
  {
    public static Sitecore.Data.ID SessionId
    {
      get
      {
        if (Settings.IsAnalyticsEnabled)
        {
          return new Sitecore.Data.ID(Tracker.CurrentVisit.VisitId);
        }
        return Sitecore.Data.ID.Null;
      }
    }

    public static System.DateTime BasePageTime
    {
      get
      {
        return (System.DateTime)(Context.Items["SC_WFM_ANALYTICS_EVENT_BASE_TIME"] ?? System.DateTime.Now);
      }
      set
      {
        Context.Items["SC_WFM_ANALYTICS_EVENT_BASE_TIME"] = value;
      }
    }

    public static int EventCounter
    {
      get
      {
        if (Settings.IsAnalyticsEnabled)
        {
          int num = (int)(Context.Items["SC_WFM_ANALYTICS_EVENT_TICKS"] ?? -1);
          int shortIntTicks;
          do
          {
            System.TimeSpan dateTime = System.DateTime.Now - AnalyticsTracker.BasePageTime;
            shortIntTicks = Sitecore.Form.Core.Utility.DateUtil.GetShortIntTicks(dateTime);
          }
          while (num == shortIntTicks);
          return shortIntTicks;
        }
        return 0;
      }
    }

    public static void AddTag(string name, string value)
    {
      Sitecore.Diagnostics.Assert.ArgumentNotNullOrEmpty(name, "name");
      if (Settings.IsAnalyticsEnabled)
      {
        Tracker.Visitor.Tags.Add(name, value ?? string.Empty);
      }
    }

    public static string GetTag(string name)
    {
      Sitecore.Diagnostics.Assert.ArgumentNotNullOrEmpty(name, "name");
      if (Settings.IsAnalyticsEnabled)
      {
        return Tracker.Visitor.Tags[name];
      }
      return null;
    }

    public static void RegisterFormDropouts(System.Web.HttpContext context)
    {
      if (Settings.IsAnalyticsEnabled && context != null)
      {
        string[] array = context.Session.Keys.Cast<string>().ToArray<string>();
        for (int i = 0; i < array.Length; i++)
        {
          string text = array[i];
          if (!string.IsNullOrEmpty(text) && text.StartsWith(Sitecore.Form.Core.Configuration.Constants.SessionFormBeginTrack))
          {
            if (!string.IsNullOrEmpty((string)context.Session[text]))
            {
              string value = text.Substring(Sitecore.Form.Core.Configuration.Constants.SessionFormBeginTrack.Length);
              string text2 = (string)context.Session[text];
              int pageNumber;
              Sitecore.Data.ID iD;
              if (text2 != "-1" && int.TryParse(text2, out pageNumber) && Sitecore.Data.ID.TryParse(value, out iD))
              {
                VisitorDataSet.PagesRow page = Tracker.CurrentVisit.GetPage(pageNumber);
                if (page != null)
                {
                  VisitorDataSet.PageEventsRow pageEventsRow = page.Register(Sitecore.Form.Core.Configuration.Constants.Analytics.FormDropoutEvent, string.Empty, IDs.FormTemplateID.ToString(), string.Empty);
                  pageEventsRow.ItemId = iD.Guid;
                }
              }
            }
            context.Session.Remove(text);
          }
        }
      }
    }

    public static void TriggerCampaign(Sitecore.Data.ID campaignId, Sitecore.Data.Database database)
    {
      Sitecore.Diagnostics.Assert.ArgumentNotNull(database, "database");
      VisitorDataSet.PagesRow currentPage = Tracker.CurrentVisit.CurrentPage;
      if (currentPage != null)
      {
        currentPage.RegisterCampaign(campaignId.Guid, database);
      }
    }

    public static void TriggerEvent(ClientEvent data, bool triggerForPreviousPage = true)
    {
      VisitorDataSet.PagesRow page = triggerForPreviousPage ? Tracker.CurrentVisit.PreviousPage : Tracker.CurrentVisit.CurrentPage;
      AnalyticsTracker.TriggerEvent(data, page);
      if (triggerForPreviousPage)
      {
        Tracker.CurrentPage.Cancel();
      }
    }

    private static void TriggerEvent(ClientEvent data, VisitorDataSet.PagesRow page)
    {
      Sitecore.Diagnostics.Assert.ArgumentNotNull(page, "page");
      Sitecore.Data.Items.Item item = Context.Database.GetItem(Sitecore.Data.ID.Parse(data.FormID));
      if (item != null && System.Web.HttpContext.Current.Request.UrlReferrer != null)
      {
        page.Url = System.Web.HttpContext.Current.Request.UrlReferrer.PathAndQuery;
        Sitecore.Data.Items.Item item2 = Context.Database.GetItem(new Sitecore.Data.ID(page.ItemId));
        if (Context.Item == null)
        {
          Context.Item = item2;
        }
        if (item2 != null)
        {
          page.ItemId = item2.ID.Guid;
          page.ItemLanguage = item2.Language.Name;
          page.ItemVersion = item2.Version.Number;
        }
        page.Register(data);
      }
    }

    public static void TriggerEvent(ServerEvent data)
    {
      if (data.Type != null)
      {
        Sitecore.Data.Items.Item item = Context.Database.GetItem(data.FormID);
        if (item != null)
        {
          VisitorDataSet.PagesRow currentPage = Tracker.CurrentVisit.CurrentPage;
          if (currentPage != null)
          {
            currentPage.Register(data);
          }
        }
      }
    }

    public static void TriggerGoal(string formID, string goalName, string goalID)
    {
      Sitecore.Diagnostics.Assert.ArgumentNotNull(formID, "formID");
      Sitecore.Data.Items.Item item = Context.Database.GetItem(formID);
      if (item != null)
      {
        VisitorDataSet.PagesRow currentPage = Tracker.CurrentVisit.CurrentPage;
        if (currentPage != null)
        {
          currentPage.RegisterGoal(goalName, formID, goalID);
        }
      }
    }

    public static void TriggerJsonEvents(string jsonTrackData)
    {
      if (!string.IsNullOrEmpty(jsonTrackData))
      {
        ClientEvent[] array = Json.Instance.DeserializeObject<ClientEvent[]>(jsonTrackData);
        ClientEvent[] array2 = array;
        for (int i = 0; i < array2.Length; i++)
        {
          ClientEvent data = array2[i];
          AnalyticsTracker.TriggerEvent(data, true);
        }
      }
    }
  }
}