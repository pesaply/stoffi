# -*- encoding : utf-8 -*-
require 'wikicloth'

class WikiParser < WikiCloth::Parser
	url_for do |page|
		"http://en.wikipedia.org/wiki/#{page}"
	end
	
	link_attributes_for do |page|
		{ :href => url_for(page) }
	end
	
	external_link do |url,text|
		"<a href='#{url}'>#{text.blank? ? url : text}</a>"
	end
	
	def self.sanitize(s)
        # strip anything inside curly braces!
        while s =~ /\{\{[^\{\}]+?\}\}/
          s.gsub!(/\{\{[^\{\}]+?\}\}/, '')
        end

        # strip info box
        s.sub!(/^\{\|[^\{\}]+?\n\|\}\n/, '')

        # strip internal links
		s.gsub!(/\[\[[a-z]{2}:[^\]]+\]\]/, '')
        s.gsub!(/\[\[([^\]\|]+?)\|([^\]\|]+?)\]\]/, '\2')
        s.gsub!(/\[\[([^\]\|]+?)\]\]/, '\1')

        # strip images and file links
        s.gsub!(/\[\[Image:[^\[\]]+?\]\]/i, '')
        s.gsub!(/\[\[File:[^\[\]]+?\]\]/i, '')

        # convert bold/italic to html
        s.gsub!(/'''''(.+?)'''''/, '<b><i>\1</i></b>')
        s.gsub!(/'''(.+?)'''/, '<b>\1</b>')
        s.gsub!(/''(.+?)''/, '<i>\1</i>')

        # misc
		s.gsub!(/<ref[^<>]*\/>/i, '')
        s.gsub!(/<ref[^<>]*>[\s\S]*?<\/ref>/i, '')
        s.gsub!(/<!--[^>]+?-->/, '')
        s.gsub!('()', '')
        s.gsub!(/ ([\,\.\?\!])/, '\1')
        s.strip
	end
end
