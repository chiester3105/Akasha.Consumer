CREATE SCHEMA IF NOT EXISTS stats;

CREATE TABLE IF NOT EXISTS stats.matches(
	id SERIAL PRIMARY KEY,
	external_id TEXT UNIQUE NOT NULL,
	server_id TEXT NOT NULL,
	map_name TEXT,
	mission_name TEXT,
	start_time BIGINT NOT NULL,
	end_time BIGINT NOT NULL,
	winner TEXT,
	duration DOUBLE PRECISION,
	primeva_score REAL,
	boscali_score REAL
);
CREATE INDEX IF NOT EXISTS idx_matches_start_time ON stats.matches(start_time);
CREATE INDEX IF NOT EXISTS idx_matches_external_id ON stats.matches(external_id);

--Players in matches. There will be another schema for players table with general info:
--(last nickname, steam id + licence owner id (for sdr conn), ip + uid + password (for udp conn after I implement it), etc) 
CREATE TABLE IF NOT EXISTS stats.players (
	steam_id BIGINT NOT NULL,
	match_id TEXT NOT NULL,
	player_name TEXT,
	faction TEXT,
	score INT,

	PRIMARY KEY (steam_id, match_id),

	CONSTRAINT fk_players_match FOREIGN KEY (match_id)
		REFERENCES stats.matches(external_id) ON DELETE CASCADE
);



CREATE TABLE IF NOT EXISTS stats.sorties (
	match_id TEXT NOT NULL,
	sortie_idx INT,
	player_steam_id BIGINT NOT NULL,
	aircraft TEXT,
	start_time BIGINT,
	end_time BIGINT,
	end_reason TEXT,

	jamming_seconds REAL,
	detected_targets INT,

	PRIMARY KEY (match_id, sortie_idx),
	FOREIGN KEY (match_id) REFERENCES stats.matches(external_id) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS idx_sorties_player_steam_id ON stats.sorties(player_steam_id);
CREATE INDEX IF NOT EXISTS idx_sorties_end_time ON stats.sorties(end_time);


CREATE TABLE IF NOT EXISTS stats.kills (
	kill_id SERIAL PRIMARY KEY,
	match_id TEXT NOT NULL,
	sortie_idx INT NOT NULL,
	killed_steam_id BIGINT,
	killed_unit_name TEXT,
	weapon TEXT,

	CONSTRAINT fk_kills_sortie FOREIGN KEY (match_id, sortie_idx)
		REFERENCES stats.sorties(match_id, sortie_idx) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_kills_match_sortie_id ON stats.kills(match_id, sortie_idx);
CREATE INDEX IF NOT EXISTS idx_kills_killed_steam_id ON stats.kills(killed_steam_id);