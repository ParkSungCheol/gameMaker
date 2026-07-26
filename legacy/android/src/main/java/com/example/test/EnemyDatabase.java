package com.example.test;

import android.content.Context;
import android.os.AsyncTask;

import androidx.annotation.NonNull;
import androidx.room.Database;
import androidx.room.Room;
import androidx.room.RoomDatabase;
import androidx.sqlite.db.SupportSQLiteDatabase;

import java.util.List;

@Database(entities = {Enemy.class}, version = 1)
public abstract class EnemyDatabase extends RoomDatabase {

    private static EnemyDatabase instance;

    public abstract EnemyDao enemyDao();

    public static synchronized EnemyDatabase getInstance(Context context) {
        if(instance == null) {
            instance = Room.databaseBuilder(context.getApplicationContext(),
                    EnemyDatabase.class, "enemy")
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
            new PopulateDbAsyncTask(instance).execute();
        }
    };

    private static class PopulateDbAsyncTask extends AsyncTask<Void, Void, Void> {
        private EnemyDao enemyDao;

        private PopulateDbAsyncTask(EnemyDatabase db) {
            enemyDao = db.enemyDao();
        }

        @Override
        protected Void doInBackground(Void... voids) {
            enemyDao.insert(new Enemy(1, 175, "yourbasic"));
            enemyDao.insert(new Enemy(2, 175, "yourtank"));
            enemyDao.insert(new Enemy(3, 175, "yourbattle"));
            enemyDao.insert(new Enemy(4, 175, "yourmass"));
            enemyDao.insert(new Enemy(5, 175, "yourbasic"));
            enemyDao.insert(new Enemy(5, 174, "yourtank"));
            enemyDao.insert(new Enemy(6, 175, "yourbasic"));
            enemyDao.insert(new Enemy(6, 170, "yourbasic"));
            enemyDao.insert(new Enemy(6, 165, "yourbasic"));
            enemyDao.insert(new Enemy(6, 160, "yourbasic"));
            enemyDao.insert(new Enemy(7, 175, "yourbasic"));
            enemyDao.insert(new Enemy(7, 174, "yourtank"));
            enemyDao.insert(new Enemy(7, 170, "yourbasic"));
            enemyDao.insert(new Enemy(7, 169, "yourtank"));
            enemyDao.insert(new Enemy(7, 165, "yourbasic"));
            enemyDao.insert(new Enemy(7, 164, "yourtank"));
            enemyDao.insert(new Enemy(7, 160, "yourbasic"));
            enemyDao.insert(new Enemy(7, 159, "yourtank"));
            enemyDao.insert(new Enemy(8, 175, "yourbasic"));
            enemyDao.insert(new Enemy(8, 173, "yourbasic"));
            enemyDao.insert(new Enemy(8, 171, "yourbasic"));
            enemyDao.insert(new Enemy(8, 169, "yourbasic"));
            enemyDao.insert(new Enemy(8, 167, "yourbasic"));
            enemyDao.insert(new Enemy(8, 166, "yourtank"));
            enemyDao.insert(new Enemy(8, 165, "yourbasic"));
            enemyDao.insert(new Enemy(9, 175, "yourbasic"));
            enemyDao.insert(new Enemy(9, 174, "yourtank"));
            enemyDao.insert(new Enemy(9, 173, "yourbasic"));
            enemyDao.insert(new Enemy(9, 171, "yourbasic"));
            enemyDao.insert(new Enemy(9, 170, "yourtank"));
            enemyDao.insert(new Enemy(9, 169, "yourbasic"));
            enemyDao.insert(new Enemy(9, 167, "yourbasic"));
            enemyDao.insert(new Enemy(9, 166, "yourtank"));
            enemyDao.insert(new Enemy(9, 165, "yourbasic"));
            return null;
        }
    }
}
