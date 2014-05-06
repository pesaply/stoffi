# -*- encoding : utf-8 -*-
class RenameUrlInSongs < ActiveRecord::Migration
  def up
	rename_column :songs, :url, :foreign_url
  end

  def down
	rename_column :songs, :foreign_url, :url
  end
end
