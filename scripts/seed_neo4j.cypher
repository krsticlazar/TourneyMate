MATCH (n) DETACH DELETE n;

CREATE CONSTRAINT user_username IF NOT EXISTS FOR (u:User) REQUIRE u.username IS UNIQUE;
CREATE CONSTRAINT team_teamId   IF NOT EXISTS FOR (t:Team) REQUIRE t.teamId IS UNIQUE;
CREATE CONSTRAINT trn_trnId     IF NOT EXISTS FOR (tr:Tournament) REQUIRE tr.tournamentId IS UNIQUE;

UNWIND [
  {username:'admin01',  displayName:'Admin_01',  password:'admin123', role:'Admin'},

  {username:'host01',   displayName:'Host_01',   password:'host123',  role:'Host'},
  {username:'host02',   displayName:'Host_02',   password:'host123',  role:'Host'},
  {username:'host03',   displayName:'Host_03',   password:'host123',  role:'Host'},
  {username:'host04',   displayName:'Host_04',   password:'host123',  role:'Host'},
  {username:'host05',   displayName:'Host_05',   password:'host123',  role:'Host'},

  {username:'viewer01', displayName:'Viewer_01', password:'view123',  role:'Viewer'},
  {username:'viewer02', displayName:'Viewer_02', password:'view123',  role:'Viewer'},
  {username:'viewer03', displayName:'Viewer_03', password:'view123',  role:'Viewer'},
  {username:'viewer04', displayName:'Viewer_04', password:'view123',  role:'Viewer'},
  {username:'viewer05', displayName:'Viewer_05', password:'view123',  role:'Viewer'},
  {username:'viewer06', displayName:'Viewer_06', password:'view123',  role:'Viewer'},
  {username:'viewer07', displayName:'Viewer_07', password:'view123',  role:'Viewer'},
  {username:'viewer08', displayName:'Viewer_08', password:'view123',  role:'Viewer'},
  {username:'viewer09', displayName:'Viewer_09', password:'view123',  role:'Viewer'},
  {username:'viewer10', displayName:'Viewer_10', password:'view123',  role:'Viewer'},
  {username:'viewer11', displayName:'Viewer_11', password:'view123',  role:'Viewer'},
  {username:'viewer12', displayName:'Viewer_12', password:'view123',  role:'Viewer'},
  {username:'viewer13', displayName:'Viewer_13', password:'view123',  role:'Viewer'},
  {username:'viewer14', displayName:'Viewer_14', password:'view123',  role:'Viewer'},
  {username:'viewer15', displayName:'Viewer_15', password:'view123',  role:'Viewer'}
] AS u
CREATE (:User {
  username: u.username,
  displayName: u.displayName,
  password: u.password,
  role: u.role
});

MATCH (u:User {role:'Viewer'})
SET u:Player
SET u.playerId = u.username
SET u.name = u.displayName;

UNWIND [
  {tournamentId:'t_fut_1', name:'Nis_5v5_Cup',   sport:'Football',   status:'Open'},
  {tournamentId:'t_bsk_1', name:'3x3_Arena',     sport:'Basketball', status:'Live'},
  {tournamentId:'t_fut_2', name:'Winter_League', sport:'Football',   status:'Finished'},
  {tournamentId:'t_chs_1', name:'Rapid_Open',    sport:'Chess',      status:'Finished'}
] AS tr
CREATE (:Tournament {
  tournamentId: tr.tournamentId,
  name: tr.name,
  sport: tr.sport,
  status: tr.status
});

UNWIND [
  {teamId:'team_ft1', name:'FT_Lions',    sport:'Football'},
  {teamId:'team_ft2', name:'FT_Wolves',   sport:'Football'},
  {teamId:'team_ft3', name:'FT_Falcons',  sport:'Football'},
  {teamId:'team_ft4', name:'FT_Tigers',   sport:'Football'},

  {teamId:'team_bk1', name:'BK_Rockets',  sport:'Basketball'},
  {teamId:'team_bk2', name:'BK_Bulls',    sport:'Basketball'},
  {teamId:'team_bk3', name:'BK_Storm',    sport:'Basketball'},
  {teamId:'team_bk4', name:'BK_Phoenix',  sport:'Basketball'},

  {teamId:'team_ch1', name:'CH_Knights',  sport:'Chess'},
  {teamId:'team_ch2', name:'CH_Bishops',  sport:'Chess'}
] AS t
CREATE (:Team { teamId: t.teamId, name: t.name, sport: t.sport });

MATCH (h1:User {username:'host01'}), (tr1:Tournament {tournamentId:'t_fut_1'})
MERGE (h1)-[:HOSTS]->(tr1);
MATCH (h3:User {username:'host03'}), (tr3:Tournament {tournamentId:'t_bsk_1'})
MERGE (h3)-[:HOSTS]->(tr3);
MATCH (h2:User {username:'host02'}), (tr2:Tournament {tournamentId:'t_fut_2'})
MERGE (h2)-[:HOSTS]->(tr2);
MATCH (h4:User {username:'host04'}), (tr4:Tournament {tournamentId:'t_chs_1'})
MERGE (h4)-[:HOSTS]->(tr4);
MATCH (h5:User {username:'host05'}), (tr1:Tournament {tournamentId:'t_fut_1'})
MERGE (h5)-[:COHOSTS]->(tr1);

UNWIND [
  {playerId:'viewer01', teamId:'team_ft1'},
  {playerId:'viewer02', teamId:'team_ft2'},
  {playerId:'viewer03', teamId:'team_ft3'},
  {playerId:'viewer04', teamId:'team_ft4'},
  {playerId:'viewer05', teamId:'team_bk1'},
  {playerId:'viewer06', teamId:'team_bk2'},
  {playerId:'viewer07', teamId:'team_bk3'},
  {playerId:'viewer08', teamId:'team_bk4'},
  {playerId:'viewer09', teamId:'team_ch1'},
  {playerId:'viewer10', teamId:'team_ch2'}
] AS c
MATCH (p:Player {playerId:c.playerId}), (t:Team {teamId:c.teamId})
MERGE (p)-[:CAPTAIN_OF]->(t);

UNWIND [
  {teamId:'team_ft1', tournamentId:'t_fut_1'},
  {teamId:'team_ft2', tournamentId:'t_fut_1'},
  {teamId:'team_ft3', tournamentId:'t_fut_1'},
  {teamId:'team_ft4', tournamentId:'t_fut_1'}
] AS a
MATCH (t:Team {teamId:a.teamId}), (tr:Tournament {tournamentId:a.tournamentId})
MERGE (t)-[r:APPLIED_FOR]->(tr)
SET r.status = 'Pending', r.createdAt = datetime();

UNWIND [
  {teamId:'team_bk1', tournamentId:'t_bsk_1'},
  {teamId:'team_bk2', tournamentId:'t_bsk_1'},
  {teamId:'team_bk3', tournamentId:'t_bsk_1'},
  {teamId:'team_bk4', tournamentId:'t_bsk_1'},

  {teamId:'team_ft1', tournamentId:'t_fut_2'},
  {teamId:'team_ft2', tournamentId:'t_fut_2'},
  {teamId:'team_ft3', tournamentId:'t_fut_2'},
  {teamId:'team_ft4', tournamentId:'t_fut_2'},

  {teamId:'team_ch1', tournamentId:'t_chs_1'},
  {teamId:'team_ch2', tournamentId:'t_chs_1'}
] AS x
MATCH (t:Team {teamId:x.teamId}), (tr:Tournament {tournamentId:x.tournamentId})
MERGE (t)-[e:ENTERS]->(tr)
SET e.approved = true, e.seed = true;
