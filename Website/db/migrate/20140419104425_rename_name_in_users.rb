class RenameNameInUsers < ActiveRecord::Migration
  def change
    rename_column :users, :name, :name_source
  end
end
