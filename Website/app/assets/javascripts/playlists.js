function removeSong(playlist_id, song_id, submit, event)
{
	event.stopPropagation();
	
	if (!submit || confirm(trans[locale]['confirm']))
	{
		if ($('#add_rem_track_'+song_id).length > 0)
			$('#add_rem_track_'+song_id).remove();
			
		var playlist = '#playlist-'+playlist_id;
		if (playlist_id == -1)
			playlist = '#new_playlist'
		else if ($(playlist).length == 0 && $('#edit_playlist_'+playlist_id).length > 0)
			playlist = '#edit_playlist_'+playlist_id;
			
		$(playlist + ' [data-object="song-'+song_id+'"]').hide('slide', { direction: 'left' });
		
		if (submit)
		{
			$.ajax({
				url: '/playlists/'+playlist_id+'.json',
				data: "songs[removed][][id]="+song_id,
				type: 'PUT',
				error: function(jqXHR)
				{
					if (jqXHR.status != 200)
						$('#song_'+song_id).slideUp();
				},
				success: function()
				{
					$('#song_'+song_id).remove();
					if ($(playlist+' [data-list="songs"] li').length == 0)
						$(playlist+' [data-field="no-songs"]').slideDown();
				}
			});
		}
		else if (playlist_id != -1)
		{
			$('#tracks_removed').append(
				"<div id='add_rem_track_"+song_id+"'>"+
				"<input type='hidden' name='songs[removed][][id]' value='"+song_id+"'/>"+
				"</div>"
			);
		}
	}
}

function addSong(playlist_id, id, path, name, length, art_url, url, artist, album, genre, submit)
{
	if ($('#add_rem_track_'+id).length > 0)
		$('#add_rem_track_'+id).remove();
		
	if (playlist_id == -1 || $('#song_'+id).length == 0)
	{
		if (art_url == "" || art_url == null)
			var picture = "/assets/media/disc.png";
		else
			var picture = art_url;

		var title = trans[locale]['playlists.songs.remove'].replace("%{song}", name);
		var e = "<li data-object=\"song-"+id+"\" style='display:none;'>";
		e += "<div class=\"delete-wrap\"><a class=\"delete\" href=\"#\" ";
		e += "onclick=\"removeSong('"+playlist_id+"', '"+id+"', "+submit+", event); return false;\" ";
		e += "title=\""+title+"\">x</a></div>";
		e += "<a href=\""+url+"\" class=\"item\">";
		e += "<img height='120' width='120' src='"+picture+"'/>";
		e += "<p data-field=\"song-name\">"+name+"</p>";
		e += "</a></li>";

		var inserted = false;
		
		var playlist = '#playlist-'+playlist_id;
		if (playlist_id == -1)
			playlist = '#new_playlist'
		else if ($(playlist).length == 0 && $('#edit_playlist_'+playlist_id).length > 0)
			playlist = '#edit_playlist_'+playlist_id;
		
		var items = $(playlist+' [data-list="songs"]').children('li')

		for (j=0; j < items.length; j++)
		{
			if (name < $(items[j]).find('p:first').text())
			{
				$(items[j]).before(e);
				inserted = true;
				break;
			}
		}

		if (!inserted)
			$(playlist+' [data-list="songs"]').append(e);
		
		$('[data-object="song-'+id+'"]').slideDown();
		if ($(playlist+' [data-list="songs"] li').length == 1)
			$(playlist+' [data-field="no-songs"]').slideUp();
	}
	
	var container = "songs[added][]";
	if (playlist_id == -1)
		container = "songs[]";
	
	$('#tracks_added').append(
		"<div id='add_rem_track_"+id+"'>"+
		"<input type='hidden' name='"+container+"[path]' value='"+path+"'/>"+
		"<input type='hidden' name='"+container+"[title]' value='"+encode(name)+"'/>"+
		"<input type='hidden' name='"+container+"[length]' value='"+length+"'/>"+
		"<input type='hidden' name='"+container+"[art_url]' value='"+art_url+"'/>"+
		"<input type='hidden' name='"+container+"[url]' value='"+url+"'/>"+
		"<input type='hidden' name='"+container+"[artist]' value='"+encode(artist)+"'/>"+
		"<input type='hidden' name='"+container+"[album]' value='"+encode(album)+"'/>"+
		"<input type='hidden' name='"+container+"[genre]' value='"+encode(genre)+"'/>"+
		"</div>"
	);
}