using System;

using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Support.Wearable.Views;
using Android.Widget;

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Android.Util;
using Android.Gms.Common.Apis;
using Android.Gms.Wearable;
using Android.Graphics; 

namespace Wear
{
    [Activity(Label = "Wear", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity,
    IResultCallback,
    IGoogleApiClientConnectionCallbacks,
    IGoogleApiClientOnConnectionFailedListener,
    IMessageApiMessageListener 
    {
        Android.Gms.Common.Apis.IGoogleApiClient client;
        private const string Tag = "WearActivity";
        private const string MessageTag = "gdgshinshu";

        public void OnConnected(Android.OS.Bundle connectionHint)
        {
            Android.Gms.Wearable.WearableClass.MessageApi.AddListener(client, this);
        }

        public void OnConnectionSuspended(int cause)
        {
        }

        public void OnConnectionFailed(Android.Gms.Common.ConnectionResult result)
        {
        }

        public void OnResult(Java.Lang.Object result)
        {
        }

        public void OnMessageReceived(IMessageEvent e)
        {
            if (MessageTag.Equals(e.Path))
            {
                var msg = System.Text.Encoding.UTF8.GetString(e.GetData());
                Log.Debug(Tag, msg);

                var ringo = msg.Split(':');
                var colorName = System.Drawing.Color.FromName(ringo[2]);
                var colorRgb = Android.Graphics.Color.Rgb(colorName.R, colorName.G, colorName.B);

                this.RunOnUiThread(() =>
                    {
                        var layout = FindViewById<LinearLayout>(Resource.Id.linear);
                        layout.SetBackgroundColor(colorRgb);

                        var status = FindViewById<TextView>(Resource.Id.status);
                        status.Text = string.Format("{0}月 - {1}", ringo[0], ringo[1]);
                        status.SetBackgroundColor(colorRgb);
                        status.SetTextColor(Color.Black);
                    });
            }
        }


        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            client = new GoogleApiClientBuilder(this)
                .AddApi(Android.Gms.Wearable.WearableClass.Api)
                .AddConnectionCallbacks(this)
                .AddOnConnectionFailedListener(this)
                .Build();
            client.Connect();

            var v = FindViewById<WatchViewStub>(Resource.Id.watch_view_stub);
            v.LayoutInflated += delegate
                {
                    var send = FindViewById<Button>(Resource.Id.send);
                    send.Click += (sender, e) =>
                        {
                            var month = new System.Random().Next(0, 11);
                            SendMessageAsync(month);
                        };
                };
        }


        public async void SendMessageAsync(int month)
        {
            var nodeIds = await Task.Run(() => NodeIds);

            foreach (var nodeId in nodeIds)
            {
                WearableClass.MessageApi.SendMessage(client, nodeId, MessageTag, Encoding.UTF8.GetBytes(month.ToString()));
            }
        }

        public ICollection<string> NodeIds
        {
            get
            {
                var results = new HashSet<string>();
                var nodes =
                    WearableClass.NodeApi.GetConnectedNodes(client).Await().JavaCast<INodeApiGetConnectedNodesResult>();

                foreach (var node in nodes.Nodes)
                {
                    results.Add(node.Id);
                }
                return results;
            }
        }
    }
}



