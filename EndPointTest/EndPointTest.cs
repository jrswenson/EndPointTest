using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;

namespace EndPointTest
{
	public class EndPointTest
	{
		private static TimeSpan defaultTimeSpan = new TimeSpan();

		private TimeSpan timeOutSpan;
		private string timeOut = "";
		public string EndPoint { get; set; }

		private HttpClient httpClient = null;

		~EndPointTest()
		{
			Debug.WriteLine("~EndPointTest");
			if (httpClient == null)
			{
				httpClient.Dispose();
				httpClient = null;
			}
		}

		public string TimeOut
		{
			get
			{
				return timeOut;
			}
			set
			{
				timeOut = value;
				if (TimeSpan.TryParse(timeOut, out timeOutSpan) == false)
				{
					timeOutSpan = new TimeSpan();
				}

			}
		}

		public bool Test()
		{
			if (httpClient == null)
			{
				httpClient = new HttpClient { Timeout = timeOutSpan.CompareTo(defaultTimeSpan) > 0 ? timeOutSpan : new TimeSpan(0, 0, 0, 3, 0) };
			}			
			
			try
			{
				var response = httpClient.GetAsync(this.EndPoint).Result;
				response.EnsureSuccessStatusCode();
			}
			catch (Exception)
			{
				return false;
			}

			return true;
		}
	}
}
