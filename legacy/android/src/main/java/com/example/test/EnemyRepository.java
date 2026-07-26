package com.example.test;

import android.app.Application;
import android.os.AsyncTask;
import android.os.Build;

import androidx.annotation.RequiresApi;

import java.util.Iterator;
import java.util.List;

public class EnemyRepository {
    private EnemyDao enemyDao;

    public EnemyRepository(Application application) {
        EnemyDatabase database = EnemyDatabase.getInstance(application);
        enemyDao = database.enemyDao();
    }

    public void insert(Enemy enemy) {
        new InsertEnemyAsyncTask(enemyDao).execute(enemy);
    }

    public void update(Enemy enemy) {
        new UpdateEnemyAsyncTask(enemyDao).execute(enemy);
    }

    public void delete(Enemy enemy) {
        new DeleteEnemyAsyncTask(enemyDao).execute(enemy);
    }

    @RequiresApi(api = Build.VERSION_CODES.N)
    public List<Enemy> findEnemyByMapNumber(int mapNumber) {
        return enemyDao.getAllEnemiesByMapNumber(mapNumber);
    }

    public List<Enemy> getAllEnemies() {
        return enemyDao.getAllEnemies();
    }

    private static class InsertEnemyAsyncTask extends AsyncTask<Enemy, Void, Void> {
        private EnemyDao enemyDao;

        private InsertEnemyAsyncTask(EnemyDao enemyDao){
            this.enemyDao = enemyDao;
        }
        @Override
        protected Void doInBackground(Enemy... enemies) {
            enemyDao.insert(enemies[0]);
            return null;
        }
    }

    private static class UpdateEnemyAsyncTask extends AsyncTask<Enemy, Void, Void> {
        private EnemyDao enemyDao;

        private UpdateEnemyAsyncTask(EnemyDao enemyDao){
            this.enemyDao = enemyDao;
        }
        @Override
        protected Void doInBackground(Enemy... enemies) {
            enemyDao.update(enemies[0]);
            return null;
        }
    }

    private static class DeleteEnemyAsyncTask extends AsyncTask<Enemy, Void, Void> {
        private EnemyDao enemyDao;

        private DeleteEnemyAsyncTask(EnemyDao enemyDao){
            this.enemyDao = enemyDao;
        }
        @Override
        protected Void doInBackground(Enemy... enemies) {
            enemyDao.delete(enemies[0]);
            return null;
        }
    }
}
