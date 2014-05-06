# -*- encoding : utf-8 -*-
module PagesHelper
	def action_bar
		actions = ""
		if @os == "windows 7"
			actions += "<div class='action' onclick='window.location = \"/download?ref=actions\";'>"
		else
			actions += "<div class='action' onclick='window.location = \"/get?ref=actions\";'>"
		end
		actions += "#{image_tag('gfx/down.png')}<br/>#{t('news.actions.download')}</div>"

		actions += "<div class='action' onclick='window.location = \"/tour?ref=actions\";'>"
		actions += "#{image_tag('gfx/tour.png')}<br/>#{t('news.actions.tour')}</div>"

		actions += "<div class='action' onclick='window.location = \"/contribute?ref=actions\";'>"
		actions += "#{image_tag('gfx/user.png')}<br/>#{t('news.actions.contribute')}</div>"

		actions += "<div class='action' onclick='window.location = \"/about?ref=actions#future\";'>"
		actions += "#{image_tag('gfx/time.png')}<br/>#{t('news.actions.future')}</div>"

		actions += "<div class='action' onclick='window.location = \"http://dev.stoffiplayer.com/issues?ref=actions\";'>"
		actions += "#{image_tag('gfx/bugs.png')}<br/>#{t('news.actions.bug')}</div>"

		actions += "<div class='action' onclick='window.location = \"http://dev.stoffiplayer.com/issues?ref=actions\";'>"
		actions += "#{image_tag('gfx/idea.png')}<br/>#{t('news.actions.feature')}</div>"

		actions += "<div style='clear: both;'></div>"
	end
end
