# -*- encoding : utf-8 -*-
class AddRefreshTokenToLinks < ActiveRecord::Migration
  def change
    add_column :links, :refresh_token, :string
    add_column :links, :token_expires_at, :datetime
  end
end
