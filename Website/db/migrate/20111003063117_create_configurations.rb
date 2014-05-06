# -*- encoding : utf-8 -*-
class CreateConfigurations < ActiveRecord::Migration
  def self.up
    create_table :configurations do |t|
      t.integer :user_id
      t.string :name
      t.string :media_state
      t.integer :current_track_id
      t.string :currently_selected_navigation
      t.string :currently_active_navigation
      t.string :shuffle
      t.string :repeat
      t.float :volume
      t.float :seek
      t.string :search_policy
      t.string :upgrade_policy
      t.string :add_policy
      t.string :play_policy
      t.integer :history_list_config_id
      t.integer :queue_list_config_id
      t.integer :files_list_config_id
      t.integer :youtube_list_config_id
      t.integer :sources_list_config_id
      t.integer :current_shortcut_profile
      t.integer :current_equalizer_profile

      t.timestamps
    end
  end

  def self.down
    drop_table :configurations
  end
end
