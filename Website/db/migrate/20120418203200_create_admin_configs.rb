# -*- encoding : utf-8 -*-
class CreateAdminConfigs < ActiveRecord::Migration
  def change
    create_table :admin_configs do |t|
      t.string :name
      t.integer :pending_donations_limit

      t.timestamps
    end
  end
end
