package com.example.test;

import android.app.Application;
import android.os.AsyncTask;
import android.os.Build;

import androidx.annotation.RequiresApi;

import com.fasterxml.jackson.databind.ObjectMapper;

import java.util.List;
import java.util.Map;

public class PlayerRepository {
    private PlayerDao playerDao;

    public PlayerRepository(Application application) {
        PlayerDatabase database = PlayerDatabase.getInstance(application);
        playerDao = database.playerDao();
    }

    public void insert(Player player) {
        new InsertPlayerAsyncTask(playerDao).execute(player);
    }

    public void update(Player player) {
        new UpdatePlayerAsyncTask(playerDao).execute(player);
    }

    public void delete(Player player) {
        new DeletePlayerAsyncTask(playerDao).execute(player);
    }

    @RequiresApi(api = Build.VERSION_CODES.N)
    public void updateMoney(String name, int money) {
        playerDao.updateMoney(name, money);
    }

    @RequiresApi(api = Build.VERSION_CODES.N)
    public int updateMapClear(String name, int mapNumber) {
        try {
            String mapClear = playerDao.getPlayer(name).getMapClear();
            ObjectMapper mapper = new ObjectMapper();
            Map<String, Integer> map = mapper.readValue(mapClear, Map.class);
            String stringMapNumber = String.valueOf(mapNumber);
            map.put(stringMapNumber, map.get(stringMapNumber)+1);
            mapClear = mapper.writeValueAsString(map);
            playerDao.updateMapClear(name, mapClear);
            return map.get(stringMapNumber);
        } catch (Exception e) {

        }
        return 0;
    }

    @RequiresApi(api = Build.VERSION_CODES.N)
    public void clear(String name, int mapNumber) {
        int clearTime = updateMapClear(name, mapNumber);
        int money = mapNumber*(11 - clearTime) <= 0? 1 : mapNumber*(11 - clearTime);
        updateMoney(name, playerDao.getPlayer(name).getMoney()+money);
    }

    public Player getPlayer(String name) {
        return playerDao.getPlayer(name);
    }

    public List<Player> getAllPlayer() {
        return playerDao.getAllPlayer();
    }

    private static class InsertPlayerAsyncTask extends AsyncTask<Player, Void, Void> {
        private PlayerDao playerDao;

        private InsertPlayerAsyncTask(PlayerDao playerDao){
            this.playerDao = playerDao;
        }
        @Override
        protected Void doInBackground(Player... players) {
            playerDao.insert(players[0]);
            return null;
        }
    }

    private static class UpdatePlayerAsyncTask extends AsyncTask<Player, Void, Void> {
        private PlayerDao playerDao;

        private UpdatePlayerAsyncTask(PlayerDao playerDao){
            this.playerDao = playerDao;
        }
        @Override
        protected Void doInBackground(Player... players) {
            playerDao.update(players[0]);
            return null;
        }
    }

    private static class DeletePlayerAsyncTask extends AsyncTask<Player, Void, Void> {
        private PlayerDao playerDao;

        private DeletePlayerAsyncTask(PlayerDao playerDao){
            this.playerDao = playerDao;
        }
        @Override
        protected Void doInBackground(Player... players) {
            playerDao.delete(players[0]);
            return null;
        }
    }
}
