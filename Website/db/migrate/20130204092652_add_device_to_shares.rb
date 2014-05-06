# -*- encoding : utf-8 -*-
class AddDeviceToShares < ActiveRecord::Migration
  def change
    add_column :shares, :device_id, :integer

  end
end
