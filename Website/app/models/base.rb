# -*- encoding : utf-8 -*-
# The common base of all resource models.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

# The collection of static function for each model.
module StaticBase

	# Decode a string for presentation.
	def self.d(str)
		return str unless str.is_a? String
		
		next_str = HTMLEntities.new.decode(str)
		while (next_str != str)
			str = next_str
			next_str = HTMLEntities.new.decode(str)
		end
		return next_str
	end
	def d(str)
		StaticBase::d(str)
	end

	# Encodes a string so it can be stored and transmitted.
	#
	# The string is first decoded until it doesn't change
	# anything and then a single encoding is performed.
	def self.e(str)
		return unless str
		
		if str.is_a? String
			str = HTMLEntities.new.encode(d(str), :decimal)
			str.gsub!(/[']/, "&#39;")
			str.gsub!(/["]/, "&#34;")
			str.gsub!(/[\\]/, "&#92;")
			return str
		end
		
		return str.map { |s| e(s) } if str.is_a?(Array)
		return str.each { |a,b| str[a] = e(b) } if str.is_a?(Hash)
		return str
	end
	def e(str)
		StaticBase::e(str)
	end
end

# The collection of member function for each model.
module Base
	extend StaticBase

	# The type of the resource.
	def kind
		self.class.name.downcase
	end

	# The base URL for all resources.
	def base_url
		"http://beta.stoffiplayer.com"
	end
	
	# The URL of the resource.
	def url
		"#{base_url}/#{kind.pluralize}/#{id}"
	end
	
	# The path to use when creating links using <tt>url_for</tt> to the resource.
	def to_param
		if display.to_s.empty?
			id.to_s
		else
			"#{id}-#{d(display).gsub(/[^a-z0-9]+/i, '-')}"
		end
	end

	# The options to use when serializing the resource.
	def serialize_options
		{
			:methods => [ :kind, :display, :url ]
		}
	end
	
	# Serializes the resource to JSON.
	def as_json(options = {})
		super(DeepMerge.deep_merge!(serialize_options, options))
	end
	
	# Serializes the resource to XML.
	def to_xml(options = {})
		super(DeepMerge.deep_merge!(serialize_options, options))
	end
	
	# Cleans a string so it can be safely stored and transmitted.
	def e(str)
		StaticBase::e(str)
	end
	
	# Decode a string for presentation.
	def d(str)
		StaticBase::d(str)
	end
end
