# -*- encoding : utf-8 -*-
class RemoveStuffFromSongs < ActiveRecord::Migration
  def up
    remove_column :songs, :play_count
    remove_column :songs, :last_played
    remove_column :songs, :views
    remove_column :songs, :last_write
    remove_column :songs, :bitrate
    remove_column :songs, :channels
    remove_column :songs, :sample_rate
    remove_column :songs, :codecs
    remove_column :songs, :source
  end

  def down
    add_column :songs, :source, :string
    add_column :songs, :codecs, :string
    add_column :songs, :sample_rate, :integer
    add_column :songs, :channels, :integer
    add_column :songs, :bitrate, :integer
    add_column :songs, :last_write, :datetime
    add_column :songs, :views, :integer
    add_column :songs, :last_played, :datetime
    add_column :songs, :play_count, :integer
  end
end
