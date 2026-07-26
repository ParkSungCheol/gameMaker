package com.example.test;

import android.content.Context;
import android.os.AsyncTask;

import androidx.annotation.NonNull;
import androidx.room.Database;
import androidx.room.Room;
import androidx.room.RoomDatabase;
import androidx.sqlite.db.SupportSQLiteDatabase;

import com.fasterxml.jackson.databind.ObjectMapper;

import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.concurrent.Executors;

@Database(entities = {Player.class}, version = 1)
public abstract class PlayerDatabase extends RoomDatabase {

    private static PlayerDatabase instance;

    public abstract PlayerDao playerDao();

    public static synchronized PlayerDatabase getInstance(Context context) {
        if(instance == null) {
            instance = Room.databaseBuilder(context.getApplicationContext(),
                    PlayerDatabase.class, "player")
                    .fallbackToDestructiveMigration()
                    .addCallback(roomCallback)
                    .build();
            SupportSQLiteDatabase db = instance.getOpenHelper().getWritableDatabase();
        }
        return instance;
    }

    private static Callback roomCallback = new Callback() {
        @Override
        public void onCreate(@NonNull SupportSQLiteDatabase db) {
            super.onCreate(db);
            new PlayerDatabase.PopulateDbAsyncTask(instance).execute();
        }
    };

    private static class PopulateDbAsyncTask extends AsyncTask<Void, Void, Void> {
        private PlayerDao playerDao;

        private PopulateDbAsyncTask(PlayerDatabase db) {
            playerDao = db.playerDao();
        }

        @Override
        protected Void doInBackground(Void... voids) {
            try {
                Map<String, Integer> map = new HashMap<String, Integer>();
                map.put("1", 0);
                map.put("2", 0);
                map.put("3", 0);
                map.put("4", 0);
                map.put("5", 0);
                map.put("6", 0);
                map.put("7", 0);
                map.put("8", 0);
                map.put("9", 0);
                ObjectMapper mapper = new ObjectMapper();
                String mapClear = mapper.writeValueAsString(map);
                playerDao.insert(new Player("A", 0, mapClear));
            } catch (Exception e) {
                e.printStackTrace();
            }
            return null;
        }
    }
}
