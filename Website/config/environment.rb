# -*- encoding : utf-8 -*-
# Load the rails application
require File.expand_path('../application', __FILE__)

#Rails.env = 'development'

# Make sure we use UTF-8
Encoding.default_external = Encoding::UTF_8
Encoding.default_internal = Encoding::UTF_8

# Initialize the rails application
Stoffi::Application.initialize!
