# encoding: UTF-8
# This file is auto-generated from the current state of the database. Instead
# of editing this file, please use the migrations feature of Active Record to
# incrementally modify your database, and then regenerate this schema definition.
#
# Note that this schema.rb definition is the authoritative source for your
# database schema. If you need to create the application database on another
# system, you should be using db:schema:load, not running all the migrations
# from scratch. The latter is a flawed and unsustainable approach (the more migrations
# you'll amass, the slower it'll run and the greater likelihood for issues).
#
# It's strongly recommended to check this file into your version control system.

ActiveRecord::Schema.define(:version => 20140419104846) do

  create_table "admin_configs", :force => true do |t|
    t.string   "name"
    t.integer  "pending_donations_limit"
    t.datetime "created_at"
    t.datetime "updated_at"
  end

  create_table "admin_translatees", :force => true do |t|
    t.string   "name"
    t.text     "description"
    t.datetime "created_at"
    t.datetime "updated_at"
    t.string   "size"
  end

  create_table "admin_translatees_admin_translatee_params", :id => false, :force => true do |t|
    t.integer "admin_translatee_id"
    t.integer "admin_translatee_param_id"
  end

  create_table "admin_translation_parameters", :force => true do |t|
    t.string   "name"
    t.string   "description"
    t.string   "example"
    t.datetime "created_at"
    t.datetime "updated_at"
  end

  create_table "albums", :force => true do |t|
    t.string   "title"
    t.integer  "year"
    t.string   "description"
    t.datetime "created_at"
    t.datetime "updated_at"
    t.string   "art_url"
  end

  create_table "albums_artists", :id => false, :force => true do |t|
    t.integer "artist_id", :null => false
    t.integer "album_id",  :null => false
  end

  add_index "albums_artists", ["artist_id", "album_id"], :name => "by_album_and_artist", :unique => true

  create_table "albums_songs", :id => false, :force => true do |t|
    t.integer "album_id"
    t.integer "song_id"
  end

  add_index "albums_songs", ["album_id", "song_id"], :name => "by_album_and_song", :unique => true

  create_table "artists", :force => true do |t|
    t.string   "name"
    t.datetime "created_at"
    t.datetime "updated_at"
    t.string   "picture"
    t.text     "description"
    t.string   "donatable_status", :default => "ok"
    t.string   "facebook"
    t.string   "twitter"
    t.string   "googleplus"
    t.string   "myspace"
    t.string   "spotify"
    t.string   "youtube"
    t.string   "soundcloud"
    t.string   "website"
    t.string   "lastfm"
  end

  add_index "artists", ["name"], :name => "by_name", :unique => true

  create_table "artists_songs", :id => false, :force => true do |t|
    t.integer "artist_id"
    t.integer "song_id"
  end

  add_index "artists_songs", ["artist_id", "song_id"], :name => "by_artist_and_song", :unique => true

  create_table "client_applications", :force => true do |t|
    t.string   "name"
    t.string   "website"
    t.string   "support_url"
    t.string   "callback_url"
    t.string   "key",          :limit => 40
    t.string   "secret",       :limit => 40
    t.integer  "user_id"
    t.datetime "created_at"
    t.datetime "updated_at"
    t.string   "icon_16"
    t.string   "icon_64"
    t.string   "description"
    t.string   "author"
    t.string   "author_url"
  end

  add_index "client_applications", ["key"], :name => "index_client_applications_on_key", :unique => true

  create_table "column_sorts", :force => true do |t|
    t.integer  "user_id"
    t.integer  "column_id"
    t.string   "field"
    t.boolean  "ascending"
    t.datetime "created_at"
    t.datetime "updated_at"
  end

  create_table "columns", :force => true do |t|
    t.integer  "user_id"
    t.integer  "list_config_id"
    t.string   "name"
    t.string   "text"
    t.string   "binding"
    t.string   "sort_field"
    t.boolean  "is_always_visible"
    t.boolean  "is_sortable"
    t.float    "width"
    t.boolean  "is_visible"
    t.string   "alignment"
    t.datetime "created_at"
    t.datetime "updated_at"
  end

  create_table "configurations", :force => true do |t|
    t.integer  "user_id"
    t.string   "name"
    t.string   "media_state"
    t.integer  "current_track_id"
    t.string   "currently_selected_navigation"
    t.string   "currently_active_navigation"
    t.string   "shuffle"
    t.string   "repeat"
    t.float    "volume"
    t.float    "seek"
    t.string   "search_policy"
    t.string   "upgrade_policy"
    t.string   "add_policy"
    t.string   "play_policy"
    t.integer  "history_list_config_id"
    t.integer  "queue_list_config_id"
    t.integer  "files_list_config_id"
    t.integer  "youtube_list_config_id"
    t.integer  "sources_list_config_id"
    t.integer  "current_shortcut_profile"
    t.integer  "current_equalizer_profile"
    t.datetime "created_at"
    t.datetime "updated_at"
  end

  create_table "devices", :force => true do |t|
    t.string   "name"
    t.integer  "user_id"
    t.string   "last_ip"
    t.string   "version"
    t.datetime "created_at"
    t.datetime "updated_at"
    t.integer  "app_id"
    t.integer  "configuration_id"
    t.string   "status",           :default => "offline"
    t.string   "channels",         :default => ""
  end

  create_table "donations", :force => true do |t|
    t.integer  "artist_id"
    t.decimal  "artist_percentage",  :precision => 10, :scale => 0
    t.decimal  "stoffi_percentage",  :precision => 10, :scale => 0
    t.decimal  "charity_percentage", :precision => 10, :scale => 0
    t.decimal  "amount",             :precision => 10, :scale => 0
    t.integer  "user_id"
    t.datetime "created_at"
    t.datetime "updated_at"
    t.string   "email"
    t.integer  "return_policy",                                     :default => 0
    t.string   "message"
    t.string   "status",                                            :default => "pending"
  end

  create_table "downloads", :force => true do |t|
    t.string   "ip"
    t.string   "channel"
    t.string   "arch"
    t.string   "file"
    t.datetime "created_at"
    t.datetime "updated_at"
  end

  create_table "equalizer_profiles", :force => true do |t|
    t.string   "name"
    t.boolean  "is_protected"
    t.string   "levels"
    t.float    "echo_level"
    t.integer  "configuration_id"
    t.datetime "created_at"
    t.datetime "updated_at"
  end

  create_table "histories", :force => true do |t|
    t.integer  "user_id"
    t.datetime "created_at"
    t.datetime "updated_at"
  end

  create_table "histories_songs", :id => false, :force => true do |t|
    t.integer "history_id"
    t.integer "song_id"
  end

  create_table "keyboard_shortcut_profiles", :force => true do |t|
    t.string   "name"
    t.boolean  "is_protected"
    t.integer  "configuration_id"
    t.datetime "created_at"
    t.datetime "updated_at"
  end

  create_table "keyboard_shortcuts", :force => true do |t|
    t.integer  "user_id"
    t.string   "name"
    t.string   "category"
    t.string   "keys"
    t.boolean  "is_global"
    t.integer  "keyboard_shortcut_profile_id"
    t.datetime "created_at"
    t.datetime "updated_at"
  end

  create_table "languages", :force => true do |t|
    t.string   "native_name"
    t.string   "english_name"
    t.string   "iso_tag"
    t.string   "ietf_tag"
    t.datetime "created_at"
    t.datetime "updated_at"
  end

  create_table "link_backlogs", :force => true do |t|
    t.integer  "link_id"
    t.integer  "resource_id"
    t.string   "resource_type"
    t.string   "error"
    t.datetime "created_at",    :null => false
    t.datetime "updated_at",    :null => false
  end

  create_table "links", :force => true do |t|
    t.integer  "user_id"
    t.string   "provider"
    t.string   "uid"
    t.boolean  "do_share"
    t.boolean  "do_listen"
    t.datetime "created_at"
    t.datetime "updated_at"
    t.boolean  "do_donate",           :default => true
    t.string   "access_token_secret"
    t.string   "access_token"
    t.boolean  "do_create_playlist",  :default => true
    t.boolean  "show_button",         :default => true
    t.string   "refresh_token"
    t.datetime "token_expires_at"
    t.string   "encrypted_uid"
  end

  create_table "list_configs", :force => true do |t|
    t.integer  "user_id"
    t.string   "selected_indices"
    t.string   "filter"
    t.boolean  "use_icons"
    t.boolean  "accept_file_drops"
    t.boolean  "is_drag_sortable"
    t.boolean  "is_click_sortable"
    t.boolean  "lock_sort_on_number"
    t.integer  "configuration_id"
    t.datetime "created_at"
    t.datetime "updated_at"
  end

  create_table "listens", :force => true do |t|
    t.integer  "user_id"
    t.integer  "song_id"
    t.integer  "playlist_id"
    t.datetime "created_at"
    t.datetime "updated_at"
    t.integer  "device_id"
    t.datetime "ended_at"
    t.integer  "album_id"
    t.integer  "album_position"
    t.datetime "started_at"
  end

  create_table "oauth_nonces", :force => true do |t|
    t.string   "nonce"
    t.integer  "timestamp"
    t.datetime "created_at"
    t.datetime "updated_at"
  end

  add_index "oauth_nonces", ["nonce", "timestamp"], :name => "index_oauth_nonces_on_nonce_and_timestamp", :unique => true

  create_table "oauth_tokens", :force => true do |t|
    t.integer  "user_id"
    t.string   "type",                  :limit => 20
    t.integer  "client_application_id"
    t.string   "token",                 :limit => 40
    t.string   "secret",                :limit => 40
    t.string   "callback_url"
    t.string   "verifier",              :limit => 20
    t.string   "scope"
    t.datetime "authorized_at"
    t.datetime "invalidated_at"
    t.datetime "valid_to"
    t.datetime "created_at"
    t.datetime "updated_at"
  end

  add_index "oauth_tokens", ["token"], :name => "index_oauth_tokens_on_token", :unique => true

  create_table "playlist_subscribers", :id => false, :force => true do |t|
    t.integer "playlist_id"
    t.integer "user_id"
  end

  add_index "playlist_subscribers", ["user_id", "playlist_id"], :name => "by_user_and_playlist", :unique => true

  create_table "playlists", :force => true do |t|
    t.string   "name"
    t.integer  "user_id"
    t.boolean  "is_public",  :default => true
    t.datetime "created_at"
    t.datetime "updated_at"
  end

  add_index "playlists", ["user_id", "name"], :name => "by_user_and_name", :unique => true

  create_table "playlists_songs", :id => false, :force => true do |t|
    t.integer "playlist_id"
    t.integer "song_id"
  end

  add_index "playlists_songs", ["playlist_id", "song_id"], :name => "by_playlist_and_song", :unique => true

  create_table "queues", :force => true do |t|
    t.integer  "user_id"
    t.datetime "created_at"
    t.datetime "updated_at"
  end

  create_table "queues_songs", :id => false, :force => true do |t|
    t.integer "queue_id"
    t.integer "song_id"
  end

  create_table "shares", :force => true do |t|
    t.integer  "user_id"
    t.integer  "playlist_id"
    t.string   "message"
    t.datetime "created_at"
    t.datetime "updated_at"
    t.integer  "device_id"
    t.integer  "resource_id"
    t.string   "resource_type"
  end

  create_table "song_relations", :force => true do |t|
    t.integer "song1_id"
    t.integer "song2_id"
    t.integer "user_id"
    t.integer "weight"
  end

  add_index "song_relations", ["song1_id"], :name => "index_song_relations_on_song1_id"
  add_index "song_relations", ["song2_id"], :name => "index_song_relations_on_song2_id"
  add_index "song_relations", ["user_id"], :name => "index_song_relations_on_user_id"

  create_table "songs", :force => true do |t|
    t.string   "title"
    t.string   "genre"
    t.float    "length"
    t.string   "path"
    t.string   "description"
    t.datetime "created_at"
    t.datetime "updated_at"
    t.integer  "score"
    t.string   "foreign_url"
    t.string   "art_url"
    t.datetime "analyzed_at"
  end

  create_table "songs_artists", :id => false, :force => true do |t|
    t.integer "song_id"
    t.integer "artist_id"
  end

  create_table "songs_users", :id => false, :force => true do |t|
    t.integer "user_id"
    t.integer "song_id"
  end

  add_index "songs_users", ["user_id", "song_id"], :name => "by_song_and_user", :unique => true

  create_table "sources", :force => true do |t|
    t.integer  "user_id"
    t.integer  "configuration_id"
    t.string   "type"
    t.string   "data"
    t.boolean  "include"
    t.datetime "created_at"
    t.datetime "updated_at"
  end

  create_table "translations", :force => true do |t|
    t.integer  "language_id"
    t.integer  "translatee_id"
    t.integer  "user_id"
    t.text     "content"
    t.datetime "created_at"
    t.datetime "updated_at"
  end

  create_table "users", :force => true do |t|
    t.string   "email"
    t.string   "encrypted_password",     :limit => 128, :default => "",    :null => false
    t.string   "reset_password_token"
    t.datetime "reset_password_sent_at"
    t.datetime "remember_created_at"
    t.integer  "sign_in_count",                         :default => 0
    t.datetime "current_sign_in_at"
    t.datetime "last_sign_in_at"
    t.string   "current_sign_in_ip"
    t.string   "last_sign_in_ip"
    t.string   "password_salt"
    t.integer  "failed_attempts",                       :default => 0
    t.string   "unlock_token"
    t.datetime "locked_at"
    t.datetime "created_at"
    t.datetime "updated_at"
    t.boolean  "admin",                                 :default => false
    t.string   "image"
    t.string   "name_source"
    t.boolean  "has_password",                          :default => true
    t.string   "custom_name"
    t.string   "show_ads",                              :default => "all"
    t.string   "unique_token"
  end

  add_index "users", ["email"], :name => "index_users_on_email", :unique => true
  add_index "users", ["reset_password_token"], :name => "index_users_on_reset_password_token", :unique => true
  add_index "users", ["unlock_token"], :name => "index_users_on_unlock_token", :unique => true

  create_table "votes", :force => true do |t|
    t.integer  "user_id"
    t.integer  "translation_id"
    t.integer  "value"
    t.datetime "created_at"
    t.datetime "updated_at"
  end

end
