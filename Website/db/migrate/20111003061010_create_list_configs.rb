# -*- encoding : utf-8 -*-
class CreateListConfigs < ActiveRecord::Migration
  def self.up
    create_table :list_configs do |t|
	  t.integer :user_id
      t.string :selected_indices
      t.string :filter
      t.boolean :use_icons
      t.boolean :accept_file_drops
      t.boolean :is_drag_sortable
      t.boolean :is_click_sortable
      t.boolean :lock_sort_on_number
	  t.integer :configuration_id

      t.timestamps
    end
  end

  def self.down
    drop_table :list_configs
  end
end
