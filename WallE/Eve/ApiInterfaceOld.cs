namespace WikiObjects.WikiInterfaces.API
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Text;
    using System.Threading;
    using System.Web;
    using Newtonsoft.Json.Linq;
    using WikiObjects.CustomTypes;
    
    using WikiObjects.Http;

    public class ApiInterface : WikiInterface
    {
        #region Fields
        private ApiModuleInfos modules;
        private StringDictionary tokens = new StringDictionary();
        #endregion

        #region public override Methods
        public override void Move(Page fromPage, string toPage, string reason, bool moveTalk, bool moveSubpages, bool leaveRedirect, WatchListOptions changeWatchList)
        {
            if (!this.tokens.ContainsKey("movetoken"))
            {
                this.GetAllTokens();
                if (!this.tokens.ContainsKey("movetoken"))
                {
                    throw new WikiException("Bot does not have the right to move pages.");
                }
            }

            var request = this.NewRequest(
                "action", "move",
                "token", this.tokens["movetoken"],
                "from", fromPage.FullPageName,
                "to", toPage,
                "reason", reason,
                "watchlist", changeWatchList.ToString().ToLowerInvariant());
            request.AddNullIf(moveTalk, "movetalk");
            request.AddNullIf(moveSubpages, "movesubpages");
            request.AddNullIf(!leaveRedirect, "noredirect");

            if (fromPage.Site.Version == new Version(1, 16))
            {
                request.Remove("watchlist");

                if (changeWatchList == WatchListOptions.NoChange)
                {
                    changeWatchList = fromPage.IsWatched ? WatchListOptions.Watch : WatchListOptions.Unwatch;
                }

                switch (changeWatchList)
                {
                    case WatchListOptions.Watch:
                        request.AddNull("watch");
                        break;
                    case WatchListOptions.Unwatch:
                        request.AddNull("unwatch");
                        break;
                    default:
                        break;
                }
            }

            var result = this.PostJ(request);
            var move = result["move"];
            if (move == null)
            {
                throw new RobbyException("Move failed in an unexpected way.");
            }

            JToken subError;
            subError = move["talkmove-error-code"];
            if (subError != null)
            {
                this.OnWarning("talkmove-error", "Move", (string)subError["talkmove-error-info"]);
            }

            subError = move["subpages"];
            if (subError != null && subError["error"] != null)
            {
                this.OnWarning("subpages-error", "Move", (string)subError["error"]["info"]);
            }
        }

        public override void Save(Page p, string editSummary, bool? isMinor, WatchListOptions changeWatchList)
        {
            if (!this.tokens.ContainsKey("edittoken"))
            {
                this.GetAllTokens();
                if (!this.tokens.ContainsKey("edittoken"))
                {
                    throw new WikiException("Bot does not have the right to edit pages.");
                }
            }

            bool tryAgain = false;
            CaptchaData captchaData = null;
            do
            {
                try
                {
                    var result = this.PostJ(request);
                    var edit = result["edit"];
                    if ((string)edit["result"] == "Failure")
                    {
                        if (edit["captcha"] != null)
                        {
                            if (captchaData == null)
                            {
                                var captcha = new CaptchaData();
                                foreach (var kvp in edit["captcha"])
                                {
                                    var prop = (JProperty)kvp;
                                    captcha.Add(prop.Name, (string)prop.Value);
                                }

                                captcha = p.OnCaptchaChallenge(captcha);
                                if (captcha.Count > 0)
                                {
                                    foreach (var kvp in captcha)
                                    {
                                        request.PostData[kvp.Key] = kvp.Value;
                                    }

                                    tryAgain = true;
                                }
                            }
                            else
                            {
                                var exc = new CaptchaException();
                                exc.CaptchaData = captchaData;
                                throw exc;
                            }
                        }
                        else
                        {
                            throw new WikiException("Unknown edit failure", (string)result);
                        }
                    }
                    else
                    {
                        /* TODO: Should we be half-altering page info, or leave it untouched and allow user to reload if they need the details? */
                        if (edit["new"] != null)
                        {
                            p.IsNew = true;
                            p.IsMissing = false;
                        }

                        p.Id = (ulong)edit["pageid"];
                        p.FullPageName = (string)edit["title"];
                        p.ContentModel = (string)edit["contentmodel"];
                        if (edit["nochange"] == null)
                        {
                            var r = p.Revisions[0];
                            if (r == null)
                            {
                                r = new Revision();
                                p.Revisions.Add(r);
                                p.ActiveRevision = r;
                            }

                            r.Id = (ulong)edit["newrevid"];
                            r.Timestamp = (DateTime)edit["newtimestamp"];
                            r.Text = p.Text;
                            p.Text = null;
                            p.StartTimestamp = r.Timestamp;
                        }
                    }
                }
                catch (WikiException e)
                {
                    if (e.ErrorCode == "editconflict")
                    {
                        throw new EditConflictException();
                    }

                    if (e.ErrorCode == "pagedeleted" && p.OnRecreateChallenge())
                    {
                        request.AddNull("recreate");
                        tryAgain = true;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            while (tryAgain);
        }
        #endregion
    }
}