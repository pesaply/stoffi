class RenameObjectInShares < ActiveRecord::Migration
  def change
    change_table :shares do |t|
      t.rename :object_type, :resource_type
      t.rename :object_id, :resource_id
	end
  end
end
