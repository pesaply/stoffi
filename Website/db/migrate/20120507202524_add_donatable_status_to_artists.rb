# -*- encoding : utf-8 -*-
class AddDonatableStatusToArtists < ActiveRecord::Migration
  def change
    add_column :artists, :donatable_status, :string, :default => "ok"
  end
end
