public class Script : ScriptBase {
	public override async Task<HttpResponseMessage> ExecuteAsync() {
		string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssffff");
		string guid = DateTime.Now.ToString("yyyyMMddHHmmssffff");
		var requestJSON = this.Context.Request.Content.ReadAsStringAsync().Result;
		this.Context.Request.Headers.TryGetValues("x-a5634chsh-required-fields", out var requiredFieldArray);
		this.Context.Request.Headers.TryGetValues("x-a5634chsh-si", out var senderId);
		this.Context.Request.Headers.TryGetValues("x-a5634chsh-sp", out var senderPassword);
		string securityWrapper = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<request>
	<control>
		<senderid>" + String.Concat(senderId) + @"</senderid>
		<password>" + String.Concat(senderPassword) + @"</password>
		<controlid>" + timestamp + @"</controlid>
		<uniqueid>false</uniqueid>
		<dtdversion>3.0</dtdversion>
		<includewhitespace>false</includewhitespace>
	</control>
	<operation>";

		string functionWrapper = "";
		string functionCall = "";
		string rootElement = this.Context.OperationId;
		rootElement = rootElement.Substring(0, 1).ToLower() + rootElement.Substring(1);
		if(this.Context.OperationId == "GenerateApiSession") {
			this.Context.Request.Headers.TryGetValues("x-a5634chsh-ui", out var userId);
			this.Context.Request.Headers.TryGetValues("x-a5634chsh-up", out var userPassword);
			this.Context.Request.Headers.TryGetValues("x-a5634chsh-ci", out var companyId);
			functionWrapper = @"
		<authentication>
			<login>
				<userid>" + String.Concat(userId) + @"</userid>
				<companyid>" + String.Concat(companyId) + @"</companyid>
				<password>" + String.Concat(userPassword) + @"</password>
			</login>
		</authentication>
		<content>
			<function controlid=""" + guid + @""">
				<getAPISession />";
		} else {
			var requestObject = JObject.Parse(requestJSON);
			this.Context.Request.Headers.TryGetValues("x-a5634chsh-id", out var headerSessionId);
			string sessionId = String.Concat(headerSessionId);
			functionWrapper = @"
		<authentication>
			<sessionid>" + String.Concat(sessionId) + @"</sessionid>
		</authentication>
		<content>
			<function controlid=""" + String.Concat(guid) + @""">";
			if(requiredFieldArray != null) {
				var requiredFieldsArray = String.Concat(requiredFieldArray).Split(',');
				foreach(string f in requiredFieldsArray) {
					if(requestObject["data"][rootElement].SelectToken(f) == null) {
						requestObject["data"][rootElement][f] = "";
					}
				}
			}
			XmlDocument requestDoc = JsonConvert.DeserializeObject<XmlDocument>(requestObject["data"].ToString());
			functionCall = requestDoc.OuterXml;
		}
		string wrapperFooter = @"
			</function>
		</content>
	</operation>
</request>";
		this.Context.Request.Content = new StringContent(securityWrapper + functionWrapper + functionCall + wrapperFooter);
		var overrideCheck = this.Context.Request.Headers.TryGetValues("x-url-override", out var urlOverride);
		if(overrideCheck) {
			this.Context.Request.Headers.TryGetValues("x-a5634chsh-url", out var baseUrl);
			this.Context.Request.RequestUri = new Uri(String.Concat(baseUrl));
		}
		this.Context.Request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");

		// send request
		HttpResponseMessage response = await this.Context.SendAsync(this.Context.Request, this.CancellationToken).ConfigureAwait(continueOnCapturedContext: false);

		var responseString = response.Content.ReadAsStringAsync().Result;
		XmlDocument responseDoc = new XmlDocument();
		responseDoc.LoadXml(responseString);
		var responseJSON = JsonConvert.SerializeXmlNode(responseDoc);
		response.Content =  new StringContent(JObject.Parse(responseJSON).ToString(), Encoding.UTF8, "application/json");
		return response;
	}
}