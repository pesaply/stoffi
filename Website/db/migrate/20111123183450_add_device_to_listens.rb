# -*- encoding : utf-8 -*-
class AddDeviceToListens < ActiveRecord::Migration
  def change
    add_column :listens, :device_id, :integer
  end
end
