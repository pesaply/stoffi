# -*- encoding : utf-8 -*-
class AddStatusToDevices < ActiveRecord::Migration
  def change
    add_column :devices, :status, :string, :default => "offline"
    add_column :devices, :channels, :string, :default => ""
  end
end
