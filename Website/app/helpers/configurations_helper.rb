# -*- encoding : utf-8 -*-
module ConfigurationsHelper
	def play_pause_image(config)
		case config.media_state
		when "Playing"
			"media/pause.png"
		else
			"media/play.png"
		end
	end
	
	def repeat_image(config)
		case config.repeat
		when "RepeatAll"
			return "media/repeat_all.png"
		when "RepeatOne"
			return "media/repeat_one.png"
		else
			return "media/repeat_off.png"
		end
	end
	def shuffle_image(config)
		case config.shuffle
		when "Random"
			"media/shuffle_on.png"
		else
			"media/shuffle_off.png"
		end
	end
end
