# -*- encoding : utf-8 -*-
class AddConfigurationsToDevices < ActiveRecord::Migration
  def change
    add_column :devices, :configuration_id, :int
  end
end
