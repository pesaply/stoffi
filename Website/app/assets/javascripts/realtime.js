realtimeReconnectInterval = null;

/*
 * Attempts to reconnect the realtime communication.
 */
function attemptReconnect()
{
	// embedded systems have their own reconnection logic
	if (typeof(embedded) != "undefined")
		return;

	try
	{
		if (realtimeReconnectInterval != null)
			clearInterval(realtimeReconnectInterval);
		realtimeReconnectInterval = setInterval(function()
		{
			$.ajax(
			{
				type: "GET",
				url: document.url,
				cache:false,
				success: function(output)
				{ 
					location.reload();
				}
			});
		}, 5000);
	}
	catch (err)
	{
		//alert("error attempting to reconnect: " + err);
	}
}

/*
 * Sets the session ID in the embedding client.
 * 
 * This is called when the real time communication channel is established
 * in order to inform the embedding client of the session ID so it can be
 * sent along any requests to prevent echoes.
 *
 * @param sessionID
 *   The ID of the communication session.
 */
function setSessionID(sessionID)
{
	realtimeSessionID = sessionID;
	try
	{
		if (typeof(embedded) != "undefined")
			window.external.SetSessionID(sessionID);
	}
	catch(err)
	{
		//console.log("could not tell embedder of session ID: " + err);
	}
}

/*
 * Connects the realtime communicationc channel.
 * 
 * Will attempt to connect the socket.io channel to the Node.js server handling
 * realtime communication.
 */
function connectRealtime()
{
	if (realtimeObject != null)
	{
		realtimeObject.unbind("connect");
		realtimeObject.unbind("disconnect");
	}
	realtimeObject = new Juggernaut({secure: true, host: "ws.stoffiplayer.com", port: 8080});
	
	realtimeObject.meta = { version: "beta" };
	if (realtimeUserID != null)
		realtimeObject.meta['user_id'] = realtimeUserID;
	if (realtimeDeviceID != null)
		realtimeObject.meta['device_id'] = realtimeDeviceID
	
	for (var i=0; i < realtimeChannels.length; i++)
	{
		realtimeObject.subscribe(realtimeChannels[i], function(data)
		{
			try { eval(data); }
			catch(err) { }
		});
	}
	
	realtimeObject.on("connect", function()
	{
		try
		{
			if (realtimeReconnectInterval != null)
				clearInterval(realtimeReconnectInterval);
			setSessionID(this.sessionID);
		}
		catch(err) { }
	});
	realtimeObject.on("disconnect", function()
	{
		try
		{
			setSessionID("");
			attemptReconnect();
		}
		catch(err) { }
	});
}
