# -*- encoding : utf-8 -*-
class SetTimestampsInListens < ActiveRecord::Migration
	def up
		Listen.all.each do |listen|
			if listen.started_at == nil
				listen.started_at = listen.created_at
			end
			if listen.ended_at == nil and listen.song != nil and listen.song.length != nil
				listen.ended_at = listen.started_at + listen.song.length
			end
			listen.save
		end
	end

	def down
	end
end
