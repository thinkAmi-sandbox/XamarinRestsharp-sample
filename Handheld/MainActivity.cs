using System;

using Android.App;
using Android.Runtime;
using Android.Widget;
using Android.OS;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Android.Util;
using Android.Gms.Common.Apis;
using Android.Gms.Wearable;

namespace Handheld
{
    [Activity(Label = "Handheld", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity,
    IResultCallback,
    IGoogleApiClientConnectionCallbacks,
    IGoogleApiClientOnConnectionFailedListener,
    IMessageApiMessageListener 
    {
        Android.Gms.Common.Apis.IGoogleApiClient client;
        private const string Tag = "HandheldActivity";
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
                var month = System.Text.Encoding.UTF8.GetString(e.GetData());
                Log.Debug(Tag, month);

                SendResponseAsync(int.Parse(month));
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

        public async void SendResponseAsync(int month)
        {
            var restClient = new RestSharp.RestClient("http://ringo-tabetter-api.herokuapp.com/");
            var request = new RestSharp.RestRequest("api/v1/month", RestSharp.Method.GET);

            var response = await restClient.ExecuteTaskAsync(request);
            RestSharp.Deserializers.JsonDeserializer deserial = new RestSharp.Deserializers.JsonDeserializer();
            var results = deserial.Deserialize<List<RingoByMonths>>(response);

            var topRingo = results.OrderByDescending(r => r.quantities[month]).First();

            this.RunOnUiThread(() =>
                {
                    var status = FindViewById<TextView>(Resource.Id.HandheldStatus);
                    status.Text = topRingo.name;
                }
            );

            var msg = (month + 1).ToString() + ":" + topRingo.name + ":" + topRingo.color;


            var nodeIds = await Task.Run(() => NodeIds);
            foreach (var nodeId in nodeIds)
            {
                WearableClass.MessageApi.SendMessage(client, nodeId, MessageTag, System.Text.Encoding.UTF8.GetBytes(msg));
            }
        }

        public class RingoByMonths
        {
            public string name { get; set; }
            public List<int> quantities { get; set; }
            public string color { get; set; }
        }
    }
}