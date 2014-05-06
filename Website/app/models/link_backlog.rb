# -*- encoding : utf-8 -*-
# The model of the link backlog resource describing the
# failed transmissions onto third party links.
#
# This code is part of the Stoffi Music Player Project.
# Visit our website at: stoffiplayer.com
#
# Author::		Christoffer Brodd-Reijer (christoffer@stoffiplayer.com)
# Copyright::	Copyright (c) 2013 Simplare
# License::		GNU General Public License (stoffiplayer.com/license)

# Describes a failed transmission of a resource (share/listen/etc) onto
# a third party link.
class LinkBacklog < ActiveRecord::Base
	belongs_to :link
	belongs_to :resource, :polymorphic => true
	attr_accessible :error, :link_id, :resource_id, :resource_type
end
